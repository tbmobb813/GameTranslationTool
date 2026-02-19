"""Translation engine with multi-provider support and glossary management."""

from __future__ import annotations

import json
import logging
import os
import re
import time
from typing import Any, Dict, List, Optional

from adapters.base import TextEntry

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# Default control-code patterns that must never be sent to the translation API
# ---------------------------------------------------------------------------
_DEFAULT_PRESERVE_PATTERNS: List[str] = [
    r"\{[A-Z_]+\}",       # {NAME}, {ITEM}, etc.
    r"\\n",                # Escaped newlines
    r"\x1B\[[0-9;]+m",    # ANSI colour codes
    r"<[^>]+>",            # HTML / XML tags
]

# ---------------------------------------------------------------------------
# Rate limiting defaults
# ---------------------------------------------------------------------------
_DEFAULT_REQUESTS_PER_MINUTE = 60
_BATCH_CHUNK_SIZE = 50


# ---------------------------------------------------------------------------
# Glossary helpers
# ---------------------------------------------------------------------------

def _load_glossary(glossary_path: str) -> Dict[str, Any]:
    """Load a JSON glossary file.  Returns an empty structure if not found."""
    if not os.path.isfile(glossary_path):
        return {"terms": {}, "preserve": []}
    with open(glossary_path, encoding="utf-8") as fh:
        return json.load(fh)


def _apply_glossary(text: str, terms: Dict[str, str]) -> str:
    """Replace source-language terms with their glossary equivalents."""
    for source, target in terms.items():
        text = text.replace(source, target)
    return text


# ---------------------------------------------------------------------------
# Control-code handling
# ---------------------------------------------------------------------------

def _extract_codes(text: str, patterns: List[str]) -> tuple[str, Dict[str, str]]:
    """Replace control codes in *text* with stable placeholders.

    Returns:
        A tuple of ``(cleaned_text, placeholder_map)`` where *placeholder_map*
        maps each placeholder back to its original code sequence.
    """
    placeholder_map: Dict[str, str] = {}
    combined = "|".join(f"(?:{p})" for p in patterns)
    counter = 0

    def _replace(match: re.Match) -> str:
        nonlocal counter
        placeholder = f"\x00CTRL{counter}\x00"
        placeholder_map[placeholder] = match.group(0)
        counter += 1
        return placeholder

    cleaned = re.sub(combined, _replace, text)
    return cleaned, placeholder_map


def _restore_codes(text: str, placeholder_map: Dict[str, str]) -> str:
    """Restore control codes that were extracted by :func:`_extract_codes`."""
    for placeholder, original in placeholder_map.items():
        text = text.replace(placeholder, original)
    return text


# ---------------------------------------------------------------------------
# Provider back-ends
# ---------------------------------------------------------------------------

def _translate_deepl(
    text: str,
    api_key: str,
    source_lang: str = "JA",
    target_lang: str = "EN-US",
) -> str:
    """Call the DeepL Free/Pro API and return the translated string."""
    import requests  # type: ignore[import]

    url = "https://api-free.deepl.com/v2/translate"
    payload = {
        "auth_key": api_key,
        "text": text,
        "source_lang": source_lang,
        "target_lang": target_lang,
    }
    response = requests.post(url, data=payload, timeout=30)
    response.raise_for_status()
    return response.json()["translations"][0]["text"]


def _translate_openai(
    text: str,
    api_key: str,
    model: str = "gpt-4o-mini",
    source_lang: str = "Japanese",
    target_lang: str = "English",
    context: Optional[Dict[str, Any]] = None,
) -> str:
    """Translate *text* using the OpenAI Chat Completions API."""
    import requests  # type: ignore[import]

    context_hint = ""
    if context:
        speaker = context.get("speaker")
        formality = context.get("formality")
        if speaker:
            context_hint += f" The speaker is {speaker}."
        if formality:
            context_hint += f" Use {formality} register."

    system_prompt = (
        f"You are a professional video game translator. "
        f"Translate the following {source_lang} text to {target_lang}. "
        f"Preserve all control codes and formatting exactly as they appear.{context_hint} "
        f"Return only the translated text, no explanations."
    )

    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json",
    }
    payload = {
        "model": model,
        "messages": [
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": text},
        ],
    }
    response = requests.post(
        "https://api.openai.com/v1/chat/completions",
        headers=headers,
        json=payload,
        timeout=60,
    )
    response.raise_for_status()
    return response.json()["choices"][0]["message"]["content"].strip()


def _translate_ollama(
    text: str,
    model: str = "mistral",
    host: str = "http://localhost:11434",
    source_lang: str = "Japanese",
    target_lang: str = "English",
) -> str:
    """Translate *text* using a locally running Ollama model."""
    import requests  # type: ignore[import]

    prompt = (
        f"Translate the following {source_lang} video game text to {target_lang}. "
        f"Preserve all control codes and formatting. "
        f"Return only the translated text.\n\n{text}"
    )
    payload = {"model": model, "prompt": prompt, "stream": False}
    response = requests.post(f"{host}/api/generate", json=payload, timeout=120)
    response.raise_for_status()
    return response.json()["response"].strip()


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------

def translate_entry(
    entry: TextEntry,
    provider: str = "deepl",
    api_key: str = "",
    glossary_path: str = "glossary.json",
    context: Optional[Dict[str, Any]] = None,
    source_lang: str = "JA",
    target_lang: str = "EN-US",
    **provider_kwargs: Any,
) -> TextEntry:
    """Translate a single :class:`~adapters.base.TextEntry`.

    The function:

    1. Loads the glossary from *glossary_path* (if it exists).
    2. Extracts control codes from the original text and replaces them with
       stable placeholders so they are not mangled by the translation API.
    3. Applies glossary term substitutions.
    4. Sends the cleaned text to the requested *provider*.
    5. Restores the original control codes.
    6. Returns a copy of *entry* with the ``translated`` field populated.

    Args:
        entry: The text entry to translate.
        provider: One of ``"deepl"``, ``"openai"``, or ``"ollama"``.
        api_key: API key (required for ``"deepl"`` and ``"openai"``).
        glossary_path: Path to a JSON glossary file.
        context: Additional context dict passed to the provider (speaker, formality…).
        source_lang: BCP-47 / DeepL source language code.
        target_lang: BCP-47 / DeepL target language code.
        **provider_kwargs: Extra keyword arguments forwarded to the provider.

    Returns:
        A new :class:`~adapters.base.TextEntry` with the ``translated`` field set.
    """
    glossary = _load_glossary(glossary_path)
    preserve_patterns = glossary.get("preserve", []) + _DEFAULT_PRESERVE_PATTERNS
    terms: Dict[str, str] = glossary.get("terms", {})

    text = entry.original
    text, placeholder_map = _extract_codes(text, preserve_patterns)
    text = _apply_glossary(text, terms)

    merged_context = {**(entry.context or {}), **(context or {})}

    if provider == "deepl":
        translated = _translate_deepl(
            text,
            api_key=api_key,
            source_lang=source_lang,
            target_lang=target_lang,
        )
    elif provider == "openai":
        translated = _translate_openai(
            text,
            api_key=api_key,
            source_lang=source_lang,
            target_lang=target_lang,
            context=merged_context,
            **provider_kwargs,
        )
    elif provider == "ollama":
        translated = _translate_ollama(
            text,
            source_lang=source_lang,
            target_lang=target_lang,
            **provider_kwargs,
        )
    else:
        raise ValueError(
            f"Unknown translation provider: {provider!r}. "
            "Choose one of: deepl, openai, ollama."
        )

    translated = _restore_codes(translated, placeholder_map)

    import dataclasses

    return dataclasses.replace(entry, translated=translated)


def translate_batch(
    entries: List[TextEntry],
    provider: str = "deepl",
    api_key: str = "",
    glossary_path: str = "glossary.json",
    context: Optional[Dict[str, Any]] = None,
    source_lang: str = "JA",
    target_lang: str = "EN-US",
    requests_per_minute: int = _DEFAULT_REQUESTS_PER_MINUTE,
    **provider_kwargs: Any,
) -> List[TextEntry]:
    """Translate a list of entries with rate limiting.

    Entries are processed in chunks.  After each chunk a short sleep is
    inserted to stay within the *requests_per_minute* limit.

    Args:
        entries: List of text entries to translate.
        provider: Translation provider (``"deepl"``, ``"openai"``, ``"ollama"``).
        api_key: API key for cloud providers.
        glossary_path: Path to the JSON glossary file.
        context: Shared context metadata passed to every entry.
        source_lang: Source language code.
        target_lang: Target language code.
        requests_per_minute: Rate limit (requests per minute).
        **provider_kwargs: Extra arguments forwarded to :func:`translate_entry`.

    Returns:
        A new list of translated :class:`~adapters.base.TextEntry` objects.
    """
    delay = 60.0 / max(requests_per_minute, 1)
    results: List[TextEntry] = []

    for i, entry in enumerate(entries):
        try:
            translated = translate_entry(
                entry,
                provider=provider,
                api_key=api_key,
                glossary_path=glossary_path,
                context=context,
                source_lang=source_lang,
                target_lang=target_lang,
                **provider_kwargs,
            )
            results.append(translated)
        except Exception as exc:  # noqa: BLE001
            logger.error("Failed to translate entry %s: %s", entry.id, exc)
            results.append(entry)

        if i < len(entries) - 1:
            time.sleep(delay)

    return results
