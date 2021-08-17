using System;
using System.Collections;
using System.Collections.Generic;
using LobbySample;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace LobbySample
{
    public class UIManager : MonoBehaviour
    {
        public static List<PlayerSlot> playerSlots;

        public List<PlayerSlotUI> playerUI;

        private void Update()
        {
            if (playerSlots == null)
                return;
            
            for (var index = 0; index < playerUI.Count; index++)
            {
                PlayerSlotUI playerSlotUI = playerUI[index];

                if (index < playerSlots.Count)
                    playerSlotUI.UpdateInformation(playerSlots[index]);
                else
                    playerSlotUI.EmptyInformation();
            }
        }
    }

    public struct PlayerSlot
    {
        public string name;
        public int owner;
        public bool ready;
    }

    public class UISystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireSingletonForUpdate<EnableLobbySample>();
        }

        protected override void OnUpdate()
        {
            List<PlayerSlot> names = new List<PlayerSlot>();

            Entities.WithoutBurst().ForEach((in LobbyScenePlayer player) =>
                {
                    names.Add(new PlayerSlot
                    {
                        name = player.name.ToString(),
                        owner = player.id,
                        ready = player.ready
                    });
                }).Run();

            names = names.OrderBy(x => x.owner).ToList();
            
            UIManager.playerSlots = names;
        }
    }
}