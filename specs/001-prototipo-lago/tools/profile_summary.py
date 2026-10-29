"""Summarize rendered T074 captures; no external dependencies or acceptance inference."""
import argparse
import csv
import json
import math
import statistics
from pathlib import Path


def describe(values):
    ordered = sorted(values)
    if not ordered:
        return {"count": 0}
    return {"count": len(ordered), "mean": statistics.mean(ordered),
            "p50": statistics.median(ordered),
            "p95": ordered[math.ceil(len(ordered) * .95) - 1],
            "p99": ordered[math.ceil(len(ordered) * .99) - 1],
            "min": ordered[0], "max": ordered[-1]}


def summarize(path):
    path = Path(path)
    metadata = json.loads(Path(str(path) + ".json").read_text(encoding="utf-8-sig"))
    if metadata["Overwritten"]:
        raise ValueError("Capture overwrote samples: " + str(path))
    with path.open(encoding="utf-8-sig", newline="") as stream:
        rows = list(csv.DictReader(stream))
    if len(rows) != metadata["Samples"]:
        raise ValueError("Incomplete CSV: " + str(path))
    with Path(str(path) + ".gpu.csv").open(encoding="utf-8-sig", newline="") as stream:
        timings = list(csv.DictReader(stream))
    marker_rows = []
    marker_path = Path(str(path) + ".markers.csv")
    if marker_path.exists():
        with marker_path.open(encoding="utf-8-sig", newline="") as stream:
            marker_rows = list(csv.DictReader(stream))
    result = {"file": str(path), "metadata": metadata, "stages": {}}
    for enemies, low, high in ((200, 260, 1740), (400, 1760, 3240)):
        stage = [r for r in rows if low <= int(r["benchmark_tick"]) < high and int(r["enemy_target"]) == enemies]
        if not stage or any(int(r["enemies_active"]) != enemies for r in stage):
            raise ValueError("Missing or inconsistent stage " + str(enemies))
        frame = [float(r["frame_ms"]) for r in stage]
        if any(not math.isfinite(x) or x <= 0 for x in frame):
            raise ValueError("Invalid frame duration")
        if any(int(r["projectiles_active"]) != 512 or int(r["pickups_active"]) != 512 or int(r["vfx_active"]) < 128 for r in stage):
            raise ValueError("Workload saturation differs from T071")
        output = {"frame_ms": describe(frame), "mean_fps": 1000 / statistics.mean(frame),
                  "over_60fps_budget_percent": 100 * sum(x > 1000 / 60 for x in frame) / len(frame),
                  "over_30fps_budget_percent": 100 * sum(x > 1000 / 30 for x in frame) / len(frame)}
        for name, mask in (("main_thread_ms", 1), ("gc_allocated_bytes", 2), ("unity_used_bytes", 4), ("gc_used_bytes", 8)):
            values = [float(r[name]) for r in stage if int(r["available_mask"]) & mask]
            output[name] = describe(values)
            if name == "gc_allocated_bytes" and values:
                output["frames_with_gc_allocation"] = sum(x > 0 for x in values)
        output["scheduler_ms"] = describe(float(r["scheduler_ms"]) for r in stage)
        output["scheduler_per_step_ms"] = describe(float(r["scheduler_ms"]) / int(r["scheduler_steps"]) for r in stage if int(r["scheduler_steps"]) > 0)
        for family in ("enemies", "projectiles", "pickups", "vfx"):
            output[family] = {key: describe(int(r[family + "_" + key]) for r in stage) for key in ("active", "capacity", "exhaustions")}
        unique = {}
        for row in timings:
            if low <= int(row["benchmark_tick"]) < high and int(row["timing_timestamp"]) > 0:
                unique.setdefault(row["timing_timestamp"], row)
        output["gpu_ms"] = describe(float(r["gpu_ms"]) for r in unique.values() if float(r["gpu_ms"]) > 0)
        output["frame_timing_cpu_ms"] = describe(float(r["cpu_ms"]) for r in unique.values() if float(r["cpu_ms"]) > 0)
        units = dict(zip(metadata.get("NativeMarkers", []), metadata.get("NativeMarkerUnits", [])))
        selected = [r for r in marker_rows if low <= int(r["tick"]) < high]
        output["native_markers_ms"] = {name: describe(float(r[name]) / 1_000_000 for r in selected if float(r[name]) >= 0)
                                       for name, unit in units.items() if unit == "TimeNanoseconds"}
        result["stages"][str(enemies)] = output
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("captures", nargs="+")
    args = parser.parse_args()
    print(json.dumps([summarize(path) for path in args.captures], indent=2))
