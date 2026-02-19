"""Tests for game format adapters."""

from __future__ import annotations

import os

import pytest

from adapters.base import BaseAdapter, TextEntry


# ---------------------------------------------------------------------------
# TextEntry tests
# ---------------------------------------------------------------------------

class TestTextEntry:
    def test_defaults(self) -> None:
        entry = TextEntry(id="test_001", original="こんにちは")
        assert entry.translated == ""
        assert entry.context is None
        assert entry.metadata is None

    def test_to_dict_roundtrip(self) -> None:
        entry = TextEntry(
            id="test_002",
            original="勇者",
            translated="Hero",
            context={"speaker": "NPC"},
            metadata={"offset": 0x100},
        )
        d = entry.to_dict()
        restored = TextEntry.from_dict(d)
        assert restored.id == entry.id
        assert restored.original == entry.original
        assert restored.translated == entry.translated
        assert restored.context == entry.context
        assert restored.metadata == entry.metadata

    def test_from_dict_missing_translated(self) -> None:
        d = {"id": "x", "original": "hello"}
        entry = TextEntry.from_dict(d)
        assert entry.translated == ""

    def test_from_dict_none_context(self) -> None:
        d = {"id": "x", "original": "hello", "context": None}
        entry = TextEntry.from_dict(d)
        assert entry.context is None


# ---------------------------------------------------------------------------
# BaseAdapter interface tests
# ---------------------------------------------------------------------------

class ConcreteAdapter(BaseAdapter):
    """Minimal concrete adapter used for interface testing."""

    def detect(self, file_path: str) -> bool:
        return file_path.endswith(".test")

    def extract(self, file_path: str):
        return [TextEntry(id="0", original="hello")]

    def rebuild(self, file_path: str, entries, output_path: str) -> bool:
        return True


class TestBaseAdapter:
    def test_detect_positive(self) -> None:
        adapter = ConcreteAdapter()
        assert adapter.detect("game.test") is True

    def test_detect_negative(self) -> None:
        adapter = ConcreteAdapter()
        assert adapter.detect("game.bin") is False

    def test_extract_returns_list(self) -> None:
        adapter = ConcreteAdapter()
        result = adapter.extract("game.test")
        assert isinstance(result, list)
        assert all(isinstance(e, TextEntry) for e in result)

    def test_rebuild_returns_bool(self) -> None:
        adapter = ConcreteAdapter()
        assert adapter.rebuild("game.test", [], "out.test") is True

    def test_cannot_instantiate_abstract(self) -> None:
        with pytest.raises(TypeError):
            BaseAdapter()  # type: ignore[abstract]


# ---------------------------------------------------------------------------
# UnityAdapter tests
# ---------------------------------------------------------------------------

class TestUnityAdapterDetect:
    def setup_method(self) -> None:
        from adapters.unity import UnityAdapter

        self.adapter = UnityAdapter()

    def test_detects_assets(self) -> None:
        assert self.adapter.detect("resources.assets") is True

    def test_detects_unity3d(self) -> None:
        assert self.adapter.detect("bundle.unity3d") is True

    def test_detects_assetbundle(self) -> None:
        assert self.adapter.detect("data.assetbundle") is True

    def test_rejects_unknown(self) -> None:
        assert self.adapter.detect("game.iso") is False

    def test_case_insensitive(self) -> None:
        assert self.adapter.detect("bundle.UNITY3D") is True

    def test_extract_missing_file(self) -> None:
        with pytest.raises(FileNotFoundError):
            self.adapter.extract("/nonexistent/path.assets")

    def test_rebuild_missing_file(self) -> None:
        with pytest.raises(FileNotFoundError):
            self.adapter.rebuild("/nonexistent/path.assets", [], "/tmp/out.assets")

    def test_extract_requires_unitypy(self, monkeypatch) -> None:
        """If UnityPy is not importable the adapter raises ImportError."""
        import builtins
        real_import = builtins.__import__

        def mock_import(name, *args, **kwargs):
            if name == "UnityPy":
                raise ImportError("mocked")
            return real_import(name, *args, **kwargs)

        monkeypatch.setattr(builtins, "__import__", mock_import)

        # Create a temporary dummy file so the file-existence check passes
        import tempfile
        with tempfile.NamedTemporaryFile(suffix=".assets", delete=False) as tmp:
            tmp_path = tmp.name
        try:
            with pytest.raises(ImportError, match="UnityPy"):
                self.adapter.extract(tmp_path)
        finally:
            os.unlink(tmp_path)
