"""Text overflow and safety validation utilities."""

from __future__ import annotations

import re
from typing import Any, Dict

from adapters.base import TextEntry

# ---------------------------------------------------------------------------
# Control code pattern used for line-count normalisation
# ---------------------------------------------------------------------------
_NEWLINE_PATTERN = re.compile(r"\\n|\r\n|\n")


def _byte_length(text: str, encoding: str = "utf-8") -> int:
    return len(text.encode(encoding, errors="replace"))


def _line_count(text: str) -> int:
    """Count logical lines, treating ``\\n`` escape sequences as line breaks."""
    return len(_NEWLINE_PATTERN.split(text))


def _control_codes(text: str) -> list[str]:
    """Return a list of all control-code-like tokens found in *text*."""
    pattern = re.compile(
        r"\{[A-Z_]+\}"         # {NAME} placeholders
        r"|\\[nrt]"             # \\n \\r \\t escape sequences
        r"|\x1B\[[0-9;]+m"     # ANSI colour sequences
        r"|<[^>]+>"             # HTML / XML tags
    )
    return pattern.findall(text)


def check_overflow(entry: TextEntry) -> Dict[str, Any]:
    """Check whether the translated text is safe to insert into the game.

    Performs three checks:

    1. **Byte-length comparison** – Japanese text uses 2–3 bytes per character
       in Shift-JIS/UTF-8, while English uses 1.  A translated string that is
       *longer in bytes* than the original may overflow a fixed-size buffer.
    2. **Line-count check** – more lines than the original may overflow a
       fixed-height dialog box.
    3. **Control-code preservation** – control codes present in the original
       must appear in the translation unchanged.

    Args:
        entry: A :class:`~adapters.base.TextEntry` whose ``translated`` field
            has been populated.

    Returns:
        A dictionary with the following keys:

        * ``overflow`` (bool) – ``True`` if any check failed.
        * ``original_bytes`` (int) – UTF-8 byte length of the original text.
        * ``translated_bytes`` (int) – UTF-8 byte length of the translated text.
        * ``original_lines`` (int) – logical line count of the original text.
        * ``translated_lines`` (int) – logical line count of the translated text.
        * ``missing_codes`` (list[str]) – control codes present in the original
          but absent from the translation.
        * ``risk_level`` (str) – ``"low"``, ``"medium"``, or ``"high"``.
    """
    original = entry.original
    translated = entry.translated or ""

    orig_bytes = _byte_length(original)
    trans_bytes = _byte_length(translated)

    orig_lines = _line_count(original)
    trans_lines = _line_count(translated)

    orig_codes = _control_codes(original)
    trans_codes = _control_codes(translated)

    # Determine missing control codes (order-insensitive)
    missing_codes: list[str] = []
    trans_codes_copy = list(trans_codes)
    for code in orig_codes:
        if code in trans_codes_copy:
            trans_codes_copy.remove(code)
        else:
            missing_codes.append(code)

    byte_overflow = trans_bytes > orig_bytes
    line_overflow = trans_lines > orig_lines
    has_missing_codes = bool(missing_codes)

    overflow = byte_overflow or line_overflow or has_missing_codes

    # Risk scoring
    score = 0
    if byte_overflow:
        ratio = trans_bytes / orig_bytes if orig_bytes else 1.0
        score += 2 if ratio > 1.5 else 1
    if line_overflow:
        score += 2 if (trans_lines - orig_lines) > 1 else 1
    if has_missing_codes:
        score += len(missing_codes)

    if score == 0:
        risk_level = "low"
    elif score <= 2:
        risk_level = "medium"
    else:
        risk_level = "high"

    return {
        "overflow": overflow,
        "original_bytes": orig_bytes,
        "translated_bytes": trans_bytes,
        "original_lines": orig_lines,
        "translated_lines": trans_lines,
        "missing_codes": missing_codes,
        "risk_level": risk_level,
    }
