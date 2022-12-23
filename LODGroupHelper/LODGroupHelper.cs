using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using FrooxEngine.UIX;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;

namespace LODGroupHelper
{
    public class LODGroupHelper : NeosMod
    {
        public override string Name => "LODGroupHelper";
        public override string Author => "esnya";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/esnya/LODGroupHelper";

        public override void OnEngineInit()
        {
            var harmony = new Harmony($"com.nekometer.esnya.{this.Name}");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(WorkerInspector))]
        private static class WorkerInspectorPatch
        {
            private static Regex lodPattern = new Regex("LOD([0-9]+)");

            private static int ParseLODLevel(string name)
            {
                var match = lodPattern.Match(name);

                if (match.Success && int.TryParse(match.Groups[1].Value, out var parsed))
                {
                    return parsed;
                }

                return -1;
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(WorkerInspector.BuildInspectorUI))]
            private static void BuildInspectorUIPostfix(Worker worker, UIBuilder ui)
            {
                if (worker is LODGroup lodGroup) BuildLODGroupInspectorUIPostfix(lodGroup, ui);
            }

            private static void BuildLODGroupInspectorUIPostfix(LODGroup lodGroup, UIBuilder ui)
            {
                var lodGroupSlot = lodGroup.Slot;

                var button = ui.Button("Setup from Slot Name");
                button.LocalPressed += (btn, data) => {
                    var lods = new List<List<MeshRenderer>>();

                    foreach (var renderer in lodGroup.Slot.GetComponentsInChildren<MeshRenderer>())
                    {
                        var level = ParseLODLevel(renderer.Slot.Name);

                        if (level < 0) continue;

                        if (level >= lods.Count) lods.Add(new List<MeshRenderer>());

                        lods[level].Add(renderer);
                    }

                    var maxLevel = lods.Count;

                    lodGroup.LODs.Clear();

                    for (var level = 0; level < maxLevel; level++)
                    {
                        var transitionHeight = (float)Math.Pow(2, -level - 2);
                        lodGroup.AddLOD(transitionHeight, lods[level].ToArray());
                    }
                };
            }
        }
    }
}
