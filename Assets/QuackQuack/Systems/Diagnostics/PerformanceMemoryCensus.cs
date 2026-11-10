using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace QuackQuack.Systems.Diagnostics
{
    // Explicit, post-measurement diagnostic. Never called from gameplay or a measured frame.
    public static class PerformanceMemoryCensus
    {
        [Serializable] private sealed class Totals
        {
            public string BuildGuid, Unity, Platform;
            public long AllocatedBytes, ReservedBytes, UnusedReservedBytes, ManagedUsedBytes, ManagedHeapBytes;
            public long GraphicsDriverBytes;
            public int Objects;
        }
        public static void Export(string prefix)
        {
            var totals = new Totals { BuildGuid = Application.buildGUID, Unity = Application.unityVersion,
                Platform = Application.platform.ToString(), AllocatedBytes = Profiler.GetTotalAllocatedMemoryLong(),
                ReservedBytes = Profiler.GetTotalReservedMemoryLong(), UnusedReservedBytes = Profiler.GetTotalUnusedReservedMemoryLong(),
                ManagedUsedBytes = Profiler.GetMonoUsedSizeLong(), ManagedHeapBytes = Profiler.GetMonoHeapSizeLong(),
                GraphicsDriverBytes = Profiler.GetAllocatedMemoryForGraphicsDriver() };
            var objects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>(); totals.Objects = objects.Length;
            var groups = new Dictionary<string, (int Count, long Bytes)>();
            using (var writer = new StreamWriter(prefix + ".objects.csv"))
            {
                writer.WriteLine("type,name,runtime_bytes,width,height,format,readable");
                foreach (var obj in objects)
                {
                    if (obj == null) continue;
                    string type = obj.GetType().FullName; long bytes = Profiler.GetRuntimeMemorySizeLong(obj);
                    groups.TryGetValue(type, out var group); groups[type] = (group.Count + 1, group.Bytes + bytes);
                    var texture = obj as Texture; var t2d = obj as Texture2D;
                    writer.WriteLine(string.Join(",", Quote(type), Quote(obj.name), bytes.ToString(CultureInfo.InvariantCulture),
                        texture != null ? texture.width.ToString() : "", texture != null ? texture.height.ToString() : "",
                        t2d != null ? t2d.format.ToString() : "", t2d != null ? t2d.isReadable.ToString() : ""));
                }
            }
            using (var writer = new StreamWriter(prefix + ".types.csv"))
            {
                writer.WriteLine("type,count,runtime_bytes");
                foreach (var pair in groups) writer.WriteLine(Quote(pair.Key) + "," + pair.Value.Count + "," + pair.Value.Bytes);
            }
            File.WriteAllText(prefix + ".memory.json", JsonUtility.ToJson(totals, true));
        }
        private static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
