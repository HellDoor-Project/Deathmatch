using Hints;
using MEC;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using RueI.Displays;
using RueI.Elements;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide
{
    class HintOverride
    {
        public class Hint
        {
            public string msg;
            public float duration = -1.0f;
        }
        class HintInfo
        {
            

            public SortedDictionary<int, Hint> active_hints = new SortedDictionary<int, Hint>();
            public Stopwatch stop_watch = new Stopwatch();
            public CoroutineHandle handle = new CoroutineHandle();

            public HintInfo()
            {
                stop_watch.Start();
            }
            

            public void Add(int id, string msg, float duration)
            {
                UpdateDuration();

                if (!active_hints.ContainsKey(id))
                    active_hints.Add(id, new Hint { msg = msg, duration = duration });
                else
                    active_hints[id] = new Hint { msg = msg, duration = duration };
            }

            public void Remove(int id)
            {
                active_hints.Remove(id);
            }

            public void Clear()
            {
                active_hints.Clear();
            }

            public void Refresh(Player player)
            {
                if (handle.IsValid)
                    Timing.KillCoroutines(handle);
                stop_watch.Restart();
                handle = Timing.RunCoroutine(_Update(player));
            }

            private void UpdateDuration()
            {
                float delta = (float)stop_watch.Elapsed.TotalSeconds;
                stop_watch.Restart();

                foreach (int id in active_hints.Keys.ToList())
                    active_hints[id].duration -= delta;
            }

            private float Update(Player player)
            {
                UpdateDuration();

                float min = 300.0f;
                bool any_active = false;
                foreach(var id in active_hints.Keys.ToList())
                {
                    if (active_hints[id].duration > 0.0f)
                    {
                        min = Math.Min(min, active_hints[id].duration);
                        any_active = true;
                    }
                    else
                        active_hints.Remove(id);

                }
                if (any_active)
                    return min;
                else
                    return -1.0f;
            }

            private IEnumerator<float> _Update(Player player)
            {
                float delta = 1.0f;
                while (delta > 0.0f)
                {
                    delta = Update(player);
                    yield return Timing.WaitForSeconds(delta);
                }
                yield break;
            }
        }

        private static Dictionary<int, HintInfo> hint_info = new Dictionary<int, HintInfo>();


        public static string GetContent(Player player)
        {
            string msg = "";
            if (hint_info.TryGetValue(player.PlayerId, out HintInfo info))
                foreach (Hint hint in info.active_hints.Values)
                    if (hint.duration > 0.0f)
                        msg += hint.msg;
            return msg;
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        void OnPlayerJoined(Player player)
        {
            if (!hint_info.ContainsKey(player.PlayerId))
                hint_info.Add(player.PlayerId, new HintInfo());
            DisplayCore core = DisplayCore.Get(player.ReferenceHub);
            Display display = new Display(core);
            DynamicHeightPlayerElement element = new DynamicHeightPlayerElement(HintOverride.GetContent, 30);
            core.Scheduler.Schedule(TimeSpan.Zero, () => display.Elements.Add(element));
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        void OnPlayerLeft(Player player)
        {
            if (hint_info.ContainsKey(player.PlayerId))
                hint_info.Remove(player.PlayerId);
        }

        public static void Add(Player player, int id, string msg, float duration)
        {
            if (hint_info.ContainsKey(player.PlayerId))
                hint_info[player.PlayerId].Add(id, msg, duration);
        }

        public static void Remove(Player player, int id)
        {
            if (hint_info.ContainsKey(player.PlayerId))
                hint_info[player.PlayerId].Remove(id);
        }

        public static void Clear(Player player)
        {
            if (hint_info.ContainsKey(player.PlayerId))
                hint_info[player.PlayerId].Clear();
        }

        public static void Refresh(Player player)
        {
            if (hint_info.ContainsKey(player.PlayerId))
                hint_info[player.PlayerId].Refresh(player);
        }

        public static void Refresh()
        {
            foreach (var p in Player.GetPlayers())
                if (p.IsReady && hint_info.ContainsKey(p.PlayerId))
                    hint_info[p.PlayerId].Refresh(p);
        }
    }
}
