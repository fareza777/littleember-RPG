using UnityEngine;
using LittleEmber.UI;

namespace LittleEmber.World
{
    /// <summary>
    /// Talkable NPC: wires its Interactable at runtime to open the dialogue box
    /// with the serialized lines (scene-safe: no delegates in scene YAML).
    /// Pauses the villager's wander while the conversation is open.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class Talkable : MonoBehaviour
    {
        public DialogueUI dialogue;
        public VillagerNpc npc;
        public DialogueLine[] lines = new DialogueLine[0];

        void Awake()
        {
            var inter = GetComponent<Interactable>();
            inter.label = "TALK";
            inter.onUse = () =>
            {
                // if a box is already (or stuck-)open, refuse before pausing the NPC —
                // pausing first and having Show() early-return would freeze them forever
                if (dialogue == null || lines.Length == 0 || DialogueUI.IsOpen) return;
                if (npc != null) npc.paused = true;
                var d = dialogue;
                bool opened = d.Show(lines, () => { if (npc != null) npc.paused = false; });
                if (!opened && npc != null) npc.paused = false;
            };
        }
    }
}
