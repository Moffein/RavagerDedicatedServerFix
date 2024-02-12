using RoR2;
using BepInEx;
using UnityEngine;

using System;
using RedGuyMod.Content.Components;

namespace R2API.Utils
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ManualNetworkRegistrationAttribute : Attribute
    {
    }
}

namespace RavagerDedicatedServerFix
{
    [BepInDependency("com.rob.Ravager", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("com.Moffein.RavagerDedicatedServerFix", "RavagerDedicatedServerFix", "1.0.0")]
    public class RavagerDedicatedServerFix : BaseUnityPlugin
    {
        private void Awake()
        {
            RoR2Application.onLoad += OnLoad;
        }

        private void OnLoad()
        {
            GameObject redGuy = BodyCatalog.FindBodyPrefab("RobRavagerBody");
            redGuy.AddComponent<RavagerFixComponent>();
        }
    }

    //The correct way to do this would be to hook the Drain Start method
    public class RavagerFixComponent : MonoBehaviour
    {
        private CharacterBody characterBody;
        private RedGuyController controller;
        private bool wasDraining = false;

        private void Awake()
        {
            controller = base.GetComponent<RedGuyController>();
            characterBody = base.GetComponent<CharacterBody>();
            if (!controller || !characterBody || !(NetworkSession.instance && NetworkSession.instance.flags.HasFlag(NetworkSession.Flags.IsDedicatedServer)))
            {
                Destroy(this);
                return;
            }
        }

        private void FixedUpdate()
        {
            bool isDraining = controller.draining;

            if (!wasDraining && isDraining)
            {
                OnDrainStart();
            }

            wasDraining = isDraining;
        }

        private void OnDrainStart()
        {
            if (controller.hasAuthority && characterBody.healthComponent)
            {
                float storedHealth = controller.storedHealth;
                float fullHealth = characterBody.healthComponent.fullHealth;

                Inventory inv = characterBody.inventory;
                bool hasDrizzleHelper = inv && inv.GetItemCount(RoR2Content.Items.DrizzlePlayerHelper) > 0;
                bool hasMonsoonHelper = inv && inv.GetItemCount(RoR2Content.Items.MonsoonPlayerHelper) > 0;

                if (hasMonsoonHelper)
                {
                    storedHealth /= 0.6f;
                }
                else if (hasDrizzleHelper)
                {
                    storedHealth /= 1.5f;
                }

                float healthFractionToHeal = storedHealth / fullHealth;
                characterBody.AddTimedBuffAuthority(RoR2Content.Buffs.CrocoRegen.buffIndex, healthFractionToHeal * 10f);
            }
        }
    }
}
