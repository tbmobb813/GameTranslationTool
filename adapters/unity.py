"""Unity asset bundle adapter for text extraction and rebuilding."""

import logging
import os
from typing import List

from adapters.base import BaseAdapter, TextEntry

logger = logging.getLogger(__name__)

# File extensions recognised as Unity asset bundles
_UNITY_EXTENSIONS = {".assets", ".unity3d", ".assetbundle"}


class UnityAdapter(BaseAdapter):
    """Extract and rebuild text assets inside Unity asset bundles.

    Requires the ``UnityPy`` package (``pip install UnityPy``).
    """

    def detect(self, file_path: str) -> bool:
        """Return ``True`` for ``.assets``, ``.unity3d``, and ``.assetbundle`` files."""
        _, ext = os.path.splitext(file_path.lower())
        return ext in _UNITY_EXTENSIONS

    def extract(self, file_path: str) -> List[TextEntry]:
        """Extract text from *TextAsset* and *MonoBehaviour* objects.

        Returns a list of :class:`~adapters.base.TextEntry` objects, one per
        discovered string value.  The entry ``id`` encodes the asset path and
        field name so that :meth:`rebuild` can locate the asset again.

        Raises:
            ImportError: If ``UnityPy`` is not installed.
            FileNotFoundError: If *file_path* does not exist.
        """
        if not os.path.isfile(file_path):
            raise FileNotFoundError(f"Asset file not found: {file_path}")

        try:
            import UnityPy  # type: ignore[import]
        except ImportError as exc:
            raise ImportError(
                "UnityPy is required for the Unity adapter. "
                "Install it with: pip install UnityPy"
            ) from exc

        env = UnityPy.load(file_path)
        entries: List[TextEntry] = []

        for path, obj in env.container.items():
            try:
                data = obj.read()
            except Exception:  # noqa: BLE001
                logger.debug("Could not read object at path %s", path)
                continue

            class_name = type(data).__name__

            if class_name == "TextAsset":
                text = getattr(data, "m_Script", None) or getattr(data, "text", None)
                if text:
                    entry = TextEntry(
                        id=f"{path}::m_Script",
                        original=text,
                        metadata={
                            "file_path": file_path,
                            "asset_path": path,
                            "field": "m_Script",
                            "class": "TextAsset",
                        },
                    )
                    entries.append(entry)

            elif class_name == "MonoBehaviour":
                for field_name in vars(data):
                    value = getattr(data, field_name, None)
                    if isinstance(value, str) and value.strip():
                        entry = TextEntry(
                            id=f"{path}::{field_name}",
                            original=value,
                            metadata={
                                "file_path": file_path,
                                "asset_path": path,
                                "field": field_name,
                                "class": "MonoBehaviour",
                            },
                        )
                        entries.append(entry)

        logger.info("Extracted %d text entries from %s", len(entries), file_path)
        return entries

    def rebuild(
        self,
        file_path: str,
        entries: List[TextEntry],
        output_path: str,
    ) -> bool:
        """Write a copy of *file_path* to *output_path* with translated text applied.

        Each entry whose ``metadata["file_path"]`` matches *file_path* is
        written back into the appropriate asset object.

        Returns:
            ``True`` on success, ``False`` if any entry could not be written.
        """
        if not os.path.isfile(file_path):
            raise FileNotFoundError(f"Asset file not found: {file_path}")

        try:
            import UnityPy  # type: ignore[import]
        except ImportError as exc:
            raise ImportError(
                "UnityPy is required for the Unity adapter. "
                "Install it with: pip install UnityPy"
            ) from exc

        env = UnityPy.load(file_path)

        # Build a lookup: asset_path -> {field -> entry}
        entry_map: dict[str, dict[str, TextEntry]] = {}
        for entry in entries:
            if not entry.translated:
                continue
            meta = entry.metadata or {}
            if meta.get("file_path") != file_path:
                continue
            asset_path = meta.get("asset_path", "")
            field = meta.get("field", "")
            entry_map.setdefault(asset_path, {})[field] = entry

        success = True
        for path, obj in env.container.items():
            if path not in entry_map:
                continue
            try:
                data = obj.read()
                class_name = type(data).__name__
                for field_name, entry in entry_map[path].items():
                    if class_name == "TextAsset" and field_name == "m_Script":
                        data.m_Script = entry.translated
                    elif class_name == "MonoBehaviour":
                        setattr(data, field_name, entry.translated)
                    else:
                        logger.warning(
                            "Cannot write field %s on %s (class=%s)",
                            field_name,
                            path,
                            class_name,
                        )
                        success = False
                        continue
                    data.save()
            except Exception as exc:  # noqa: BLE001
                logger.error("Failed to rebuild object at %s: %s", path, exc)
                success = False

        os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
        with open(output_path, "wb") as fh:
            for obj in env.file.objects.values():
                fh.write(obj.get_raw_data())

        logger.info("Rebuilt asset written to %s", output_path)
        return success
