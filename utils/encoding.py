"""Encoding transform utilities (Shift-JIS ↔ UTF-8)."""

from __future__ import annotations

import logging
from typing import Optional

logger = logging.getLogger(__name__)

# Map of common Shift-JIS variant codec names accepted by Python
_SHIFT_JIS_CODEC = "shift_jis_2004"  # most complete variant
_UTF8_CODEC = "utf-8"


def detect_encoding(data: bytes) -> str:
    """Best-effort detection of the encoding used in *data*.

    Tries UTF-8 first (with BOM), then Shift-JIS, and falls back to
    ``"latin-1"`` (which never raises on arbitrary bytes).

    Returns:
        A Python codec name string.
    """
    # UTF-8 BOM
    if data.startswith(b"\xef\xbb\xbf"):
        return "utf-8-sig"

    # Try strict UTF-8
    try:
        data.decode("utf-8")
        return "utf-8"
    except UnicodeDecodeError:
        pass

    # Try Shift-JIS
    for codec in ("shift_jis_2004", "shift_jisx0213", "shift_jis", "cp932"):
        try:
            data.decode(codec)
            return codec
        except (UnicodeDecodeError, LookupError):
            continue

    return "latin-1"


def to_utf8(
    data: bytes,
    source_encoding: Optional[str] = None,
    errors: str = "replace",
) -> str:
    """Decode *data* to a Python ``str`` (UTF-8 internally).

    Args:
        data: Raw bytes to decode.
        source_encoding: Source codec.  If ``None`` the encoding is
            auto-detected via :func:`detect_encoding`.
        errors: Error handler passed to :meth:`bytes.decode`
            (``"strict"``, ``"replace"``, or ``"ignore"``).

    Returns:
        A decoded Python string.
    """
    if source_encoding is None:
        source_encoding = detect_encoding(data)
    return data.decode(source_encoding, errors=errors)


def to_shift_jis(
    text: str,
    target_encoding: str = _SHIFT_JIS_CODEC,
    errors: str = "replace",
) -> bytes:
    """Encode a Python string to Shift-JIS bytes.

    Args:
        text: Unicode string to encode.
        target_encoding: Target Shift-JIS codec variant.
        errors: Error handler (``"strict"``, ``"replace"``, or ``"ignore"``).

    Returns:
        Encoded bytes.
    """
    try:
        return text.encode(target_encoding, errors=errors)
    except LookupError:
        # Fall back to the most common Shift-JIS codec
        logger.warning(
            "Codec %r not available; falling back to cp932.", target_encoding
        )
        return text.encode("cp932", errors=errors)
