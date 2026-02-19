"""Patch generation wrapper supporting xdelta, BPS, and IPS formats."""

from __future__ import annotations

import logging
import os
import shutil
import subprocess
from typing import Dict

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# Internal helpers
# ---------------------------------------------------------------------------

def _require_tool(name: str) -> str:
    """Return the path to *name* or raise ``RuntimeError`` if not found."""
    path = shutil.which(name)
    if path is None:
        raise RuntimeError(
            f"Required external tool not found in PATH: {name!r}. "
            "Please install it before running patch generation."
        )
    return path


def _file_size(path: str) -> int:
    return os.path.getsize(path)


def _compression_stats(
    original_path: str,
    output_path: str,
) -> Dict[str, object]:
    orig_size = _file_size(original_path)
    patch_size = _file_size(output_path)
    ratio = (patch_size / orig_size * 100) if orig_size else 0.0
    return {
        "original_bytes": orig_size,
        "patch_bytes": patch_size,
        "ratio_pct": round(ratio, 2),
    }


# ---------------------------------------------------------------------------
# xdelta
# ---------------------------------------------------------------------------

def _generate_xdelta(original_path: str, modified_path: str, output_path: str) -> bool:
    tool = _require_tool("xdelta3")
    cmd = [tool, "-e", "-s", original_path, modified_path, output_path]
    result = subprocess.run(cmd, capture_output=True)  # noqa: S603
    if result.returncode != 0:
        logger.error("xdelta3 error: %s", result.stderr.decode(errors="replace"))
        return False
    return True


def _validate_xdelta(original_path: str, output_path: str) -> bool:
    """Apply the patch to a temp file and verify the output is non-empty."""
    tool = _require_tool("xdelta3")
    import tempfile

    with tempfile.NamedTemporaryFile(delete=False) as tmp:
        tmp_path = tmp.name

    try:
        cmd = [tool, "-d", "-s", original_path, output_path, tmp_path]
        result = subprocess.run(cmd, capture_output=True)  # noqa: S603
        if result.returncode != 0:
            logger.error(
                "Patch validation failed: %s", result.stderr.decode(errors="replace")
            )
            return False
        return os.path.getsize(tmp_path) > 0
    finally:
        if os.path.exists(tmp_path):
            os.unlink(tmp_path)


# ---------------------------------------------------------------------------
# IPS
# ---------------------------------------------------------------------------

def _generate_ips(original_path: str, modified_path: str, output_path: str) -> bool:
    """Generate a simple IPS patch (naive byte-differ, 24-bit offset limit)."""
    with open(original_path, "rb") as fh:
        orig = bytearray(fh.read())
    with open(modified_path, "rb") as fh:
        modified = bytearray(fh.read())

    records: list[bytes] = []
    i = 0
    max_len = max(len(orig), len(modified))

    while i < max_len:
        if i < len(orig) and i < len(modified) and orig[i] == modified[i]:
            i += 1
            continue

        # Start of a differing run
        start = i
        chunk: list[int] = []
        while i < max_len and len(chunk) < 0xFFFF:
            byte = modified[i] if i < len(modified) else 0x00
            orig_byte = orig[i] if i < len(orig) else 0x00
            if byte == orig_byte and len(chunk) >= 3:
                break
            chunk.append(byte)
            i += 1

        if start > 0xFFFFFF:
            logger.warning("IPS offset %d exceeds 24-bit limit; skipping record.", start)
            continue

        offset_bytes = start.to_bytes(3, "big")
        length_bytes = len(chunk).to_bytes(2, "big")
        records.append(offset_bytes + length_bytes + bytes(chunk))

    with open(output_path, "wb") as fh:
        fh.write(b"PATCH")
        for record in records:
            fh.write(record)
        fh.write(b"EOF")

    return True


# ---------------------------------------------------------------------------
# BPS
# ---------------------------------------------------------------------------

def _generate_bps(original_path: str, modified_path: str, output_path: str) -> bool:
    """Generate a BPS patch using the ``flips`` command-line tool."""
    tool = _require_tool("flips")
    cmd = [tool, "--create", "--bps", original_path, modified_path, output_path]
    result = subprocess.run(cmd, capture_output=True)  # noqa: S603
    if result.returncode != 0:
        logger.error("flips error: %s", result.stderr.decode(errors="replace"))
        return False
    return True


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------

def generate_patch(
    original_path: str,
    modified_path: str,
    output_path: str,
    format: str = "xdelta",  # noqa: A002
) -> bool:
    """Generate a binary patch from *original_path* to *modified_path*.

    Args:
        original_path: Path to the unmodified (original) file.
        modified_path: Path to the modified file.
        output_path: Destination path for the generated patch file.
        format: Patch format.  One of ``"xdelta"`` (default), ``"ips"``,
            or ``"bps"``.

    Returns:
        ``True`` if the patch was generated (and validated) successfully,
        ``False`` otherwise.

    Raises:
        FileNotFoundError: If *original_path* or *modified_path* do not exist.
        ValueError: If *format* is not recognised.
        RuntimeError: If a required external tool is missing.
    """
    for path in (original_path, modified_path):
        if not os.path.isfile(path):
            raise FileNotFoundError(f"Input file not found: {path}")

    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)

    if format == "xdelta":
        ok = _generate_xdelta(original_path, modified_path, output_path)
        if ok and os.path.isfile(output_path):
            ok = _validate_xdelta(original_path, output_path)
    elif format == "ips":
        ok = _generate_ips(original_path, modified_path, output_path)
    elif format == "bps":
        ok = _generate_bps(original_path, modified_path, output_path)
    else:
        raise ValueError(
            f"Unknown patch format: {format!r}. Choose one of: xdelta, ips, bps."
        )

    if ok and os.path.isfile(output_path):
        stats = _compression_stats(original_path, output_path)
        logger.info(
            "Patch created: %s  |  original=%d B  patch=%d B  ratio=%.1f%%",
            output_path,
            stats["original_bytes"],
            stats["patch_bytes"],
            stats["ratio_pct"],
        )
    elif not ok:
        logger.error("Patch generation failed for format %s", format)

    return ok
