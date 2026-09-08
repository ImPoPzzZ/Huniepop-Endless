using System.Linq;
using UnityEngine;

namespace HuniepopEndless
{
    /// <summary>
    /// Always-on run status panel (IMGUI, top-right). Shows the current date, goal and
    /// move count, and a running list of every permanent effect the player has drafted —
    /// buffs and debuffs — so nothing is invisible. Toggle with H.
    /// </summary>
    internal static class EndlessHud
    {
        private static bool _hidden;
        private static GUIStyle _title, _line, _dim, _box;

        internal static void Draw()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.H)
                _hidden = !_hidden;

            EnsureStyles();

            if (_hidden)
            {
                GUI.Label(new Rect(Screen.width - 150f, 8f, 150f, 20f), "Endless — [H] show", _dim);
                return;
            }

            var st = Hp2.Status;
            var m = EndlessRun.Mods;

            float w = 320f;
            float x = Screen.width - w - 10f;
            float y = 10f;

            var lines = new System.Collections.Generic.List<string>();
            lines.Add("DATE " + (EndlessRun.DatesCompleted + 1));
            if (st != null) lines.Add("Goal " + st.affectionGoal + "     Moves " + st.movesRemaining);

            lines.Add("");
            lines.Add("ACTIVE EFFECTS");
            if (m.ActiveEffects.Count == 0) lines.Add("  (none yet)");
            else foreach (var e in m.ActiveEffects) lines.Add("  + " + e);

            if (m.MovesPerDate != 0) lines.Add("  + " + m.MovesPerDate + " moves every date");
            if (m.GoalPercentAdd > 0f) lines.Add("  - goals +" + Mathf.RoundToInt(m.GoalPercentAdd * 100f) + "%");

            var bag = EndlessRun.PermaBaggage.Where(kv => kv.Value.Count > 0).ToList();
            if (bag.Count > 0)
            {
                lines.Add("");
                lines.Add("PERMANENT BAGGAGE");
                foreach (var kv in bag)
                    lines.Add("  " + Short(kv.Key.name) + ": " + kv.Value.Count
                        + (EndlessRun.PassionPenalty.TryGetValue(kv.Key, out var p) && p > 0 ? "  (-" + p + " passion cap)" : ""));
            }

            var gifts = EndlessRun.PermaGifts.Where(kv => kv.Value.Count > 0).ToList();
            if (gifts.Count > 0)
            {
                lines.Add("");
                lines.Add("PERMANENT GIFTS");
                foreach (var kv in gifts)
                    lines.Add("  " + Short(kv.Key.name) + ": " + kv.Value.Count);
            }

            float h = 14f + lines.Count * 16f;
            GUI.Box(new Rect(x, y, w, h), GUIContent.none, _box);
            float ly = y + 8f;
            foreach (var s in lines)
            {
                var style = s == "" ? _line
                    : (s == s.ToUpper() && !s.StartsWith("  ")) ? _title
                    : s.Contains("-") && s.StartsWith("  -") ? _dim : _line;
                GUI.Label(new Rect(x + 12f, ly, w - 20f, 16f), s, style);
                ly += 16f;
            }
            GUI.Label(new Rect(x + 12f, y + h - 2f, w, 16f), "[H] hide", _dim);
        }

        private static string Short(string n)
        {
            int dot = n.LastIndexOf('.');
            return dot >= 0 && dot < n.Length - 1 ? n.Substring(dot + 1) : n;
        }

        private static void EnsureStyles()
        {
            if (_box != null) return;
            _box = new GUIStyle(GUI.skin.box);
            _title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 12, normal = { textColor = new Color(0.96f, 0.55f, 0.72f) } };
            _line = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = Color.white } };
            _dim = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = new Color(1f, 1f, 1f, 0.5f) } };
        }
    }
}
