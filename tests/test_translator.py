"""Tests for the translation engine and glossary support."""

from __future__ import annotations

import json
import os
import tempfile
from typing import Any, Dict
from unittest.mock import MagicMock, patch

import pytest

from adapters.base import TextEntry
from core.translator import (
    _apply_glossary,
    _extract_codes,
    _load_glossary,
    _restore_codes,
    translate_batch,
    translate_entry,
)


# ---------------------------------------------------------------------------
# Glossary helpers
# ---------------------------------------------------------------------------

class TestLoadGlossary:
    def test_returns_empty_on_missing_file(self) -> None:
        result = _load_glossary("/nonexistent/glossary.json")
        assert result == {"terms": {}, "preserve": []}

    def test_loads_valid_file(self, tmp_path) -> None:
        data = {"terms": {"勇者": "Hero"}, "preserve": [r"\{[A-Z_]+\}"]}
        gfile = tmp_path / "glossary.json"
        gfile.write_text(json.dumps(data), encoding="utf-8")
        loaded = _load_glossary(str(gfile))
        assert loaded["terms"]["勇者"] == "Hero"
        assert r"\{[A-Z_]+\}" in loaded["preserve"]


class TestApplyGlossary:
    def test_replaces_term(self) -> None:
        result = _apply_glossary("勇者が戦う", {"勇者": "Hero"})
        assert result == "Heroが戦う"

    def test_no_match_unchanged(self) -> None:
        result = _apply_glossary("hello world", {"勇者": "Hero"})
        assert result == "hello world"

    def test_multiple_terms(self) -> None:
        result = _apply_glossary("ポーション と 勇者", {"ポーション": "Potion", "勇者": "Hero"})
        assert "Potion" in result
        assert "Hero" in result


# ---------------------------------------------------------------------------
# Control code extraction / restoration
# ---------------------------------------------------------------------------

class TestControlCodes:
    def test_extracts_placeholder(self) -> None:
        text = "Hello {NAME}, welcome!"
        cleaned, mapping = _extract_codes(text, [r"\{[A-Z_]+\}"])
        assert "{NAME}" not in cleaned
        assert len(mapping) == 1
        restored = _restore_codes(cleaned, mapping)
        assert restored == text

    def test_extracts_newline_escape(self) -> None:
        text = r"Line one\nLine two"
        cleaned, mapping = _extract_codes(text, [r"\\n"])
        assert r"\n" not in cleaned
        restored = _restore_codes(cleaned, mapping)
        assert restored == text

    def test_multiple_codes(self) -> None:
        text = r"Hello {NAME}\nGoodbye {ITEM}"
        patterns = [r"\{[A-Z_]+\}", r"\\n"]
        cleaned, mapping = _extract_codes(text, patterns)
        assert "{NAME}" not in cleaned
        assert r"\n" not in cleaned
        assert "{ITEM}" not in cleaned
        restored = _restore_codes(cleaned, mapping)
        assert restored == text

    def test_no_codes_unchanged(self) -> None:
        text = "Plain text without any codes."
        cleaned, mapping = _extract_codes(text, [r"\{[A-Z_]+\}"])
        assert cleaned == text
        assert mapping == {}


# ---------------------------------------------------------------------------
# translate_entry – unit tests with mocked providers
# ---------------------------------------------------------------------------

class TestTranslateEntry:
    def _make_entry(self, original: str = "テスト", id: str = "001") -> TextEntry:
        return TextEntry(id=id, original=original)

    def test_deepl_provider_called(self, tmp_path) -> None:
        entry = self._make_entry()
        with patch("core.translator._translate_deepl", return_value="Test") as mock:
            result = translate_entry(
                entry,
                provider="deepl",
                api_key="key",
                glossary_path=str(tmp_path / "missing.json"),
            )
        mock.assert_called_once()
        assert result.translated == "Test"

    def test_openai_provider_called(self, tmp_path) -> None:
        entry = self._make_entry()
        with patch("core.translator._translate_openai", return_value="Test OAI") as mock:
            result = translate_entry(
                entry,
                provider="openai",
                api_key="key",
                glossary_path=str(tmp_path / "missing.json"),
            )
        mock.assert_called_once()
        assert result.translated == "Test OAI"

    def test_ollama_provider_called(self, tmp_path) -> None:
        entry = self._make_entry()
        with patch("core.translator._translate_ollama", return_value="Test Ollama") as mock:
            result = translate_entry(
                entry,
                provider="ollama",
                glossary_path=str(tmp_path / "missing.json"),
            )
        mock.assert_called_once()
        assert result.translated == "Test Ollama"

    def test_unknown_provider_raises(self, tmp_path) -> None:
        entry = self._make_entry()
        with pytest.raises(ValueError, match="Unknown translation provider"):
            translate_entry(
                entry,
                provider="bogus",
                glossary_path=str(tmp_path / "missing.json"),
            )

    def test_control_codes_preserved(self, tmp_path) -> None:
        entry = self._make_entry(original=r"こんにちは {NAME}\nさようなら")
        with patch("core.translator._translate_deepl", return_value="Hello CTRL0 CTRL1 Goodbye"):
            # The mock returns a string with placeholders that will NOT be restored
            # because the real restore step uses the actual placeholder keys.
            # We patch at a higher level to test the full pipeline.
            pass

        # Use a real-ish mock that returns the cleaned text unchanged
        def fake_deepl(text, **kwargs):
            # Return the text as-is (already has placeholders substituted)
            return text

        with patch("core.translator._translate_deepl", side_effect=fake_deepl):
            result = translate_entry(
                entry,
                provider="deepl",
                api_key="key",
                glossary_path=str(tmp_path / "missing.json"),
            )

        assert "{NAME}" in result.translated
        assert r"\n" in result.translated

    def test_glossary_applied(self, tmp_path) -> None:
        glossary = {"terms": {"勇者": "Hero"}, "preserve": []}
        gfile = tmp_path / "glossary.json"
        gfile.write_text(json.dumps(glossary), encoding="utf-8")

        entry = self._make_entry(original="勇者が戦う")

        def fake_deepl(text, **kwargs):
            # The glossary replaces 勇者 with Hero before sending to API
            assert "Hero" in text
            return text

        with patch("core.translator._translate_deepl", side_effect=fake_deepl):
            translate_entry(
                entry,
                provider="deepl",
                api_key="key",
                glossary_path=str(gfile),
            )

    def test_original_entry_unchanged(self, tmp_path) -> None:
        """translate_entry must return a NEW entry; the original is immutable."""
        entry = TextEntry(id="x", original="テスト")
        with patch("core.translator._translate_deepl", return_value="Test"):
            result = translate_entry(
                entry,
                provider="deepl",
                api_key="k",
                glossary_path=str(tmp_path / "missing.json"),
            )
        assert entry.translated == ""
        assert result.translated == "Test"
        assert result is not entry


# ---------------------------------------------------------------------------
# translate_batch
# ---------------------------------------------------------------------------

class TestTranslateBatch:
    def test_returns_same_count(self, tmp_path) -> None:
        entries = [TextEntry(id=str(i), original=f"text{i}") for i in range(3)]
        with patch("core.translator.translate_entry", side_effect=lambda e, **kw: e):
            result = translate_batch(
                entries,
                provider="deepl",
                glossary_path=str(tmp_path / "missing.json"),
                requests_per_minute=600,
            )
        assert len(result) == 3

    def test_failed_entry_kept_as_original(self, tmp_path) -> None:
        entries = [TextEntry(id="fail", original="テスト")]

        def boom(entry, **kwargs):
            raise RuntimeError("API down")

        with patch("core.translator.translate_entry", side_effect=boom):
            result = translate_batch(
                entries,
                provider="deepl",
                glossary_path=str(tmp_path / "missing.json"),
                requests_per_minute=600,
            )

        assert len(result) == 1
        assert result[0].translated == ""
