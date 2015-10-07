﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;

namespace tryCassiopeia
{
    class Program
    {
        public static Menu mainMenu, comboMenu, harassMenu, ultimateMenu;
        public static Spell.Skillshot Q;
        public static Spell.Skillshot W;
        public static Spell.Targeted E;
        public static Spell.Skillshot R;

        private static long LastQ = 0;
        private static long LastE = 0;

        private static AIHeroClient _target;

        public static AIHeroClient myHero { get { return ObjectManager.Player; } }

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnComplete;
        }

        private static void OnComplete(EventArgs args)
        {
            /*
            ItemManager.Init();
            TargetSelector2.init();
            */

            if (myHero.ChampionName != "Cassiopeia")
                return;

            Q = new Spell.Skillshot(SpellSlot.Q, Q.Range, SkillShotType.Circular, 750, int.MaxValue, 150);
            W = new Spell.Skillshot(SpellSlot.W, W.Range, SkillShotType.Circular, 250, 2500, 250);
            E = new Spell.Targeted(SpellSlot.E, E.Range);
            R = new Spell.Skillshot(SpellSlot.R, R.Range, SkillShotType.Cone, (int)0.6f, int.MaxValue, (int)(80 * Math.PI / 180));

            mainMenu = MainMenu.AddMenu("tryCassiopeia", "tryCassiopeia");
            mainMenu.AddGroupLabel("tryCassiopeia");
            mainMenu.AddLabel("Made by Tryller");
            mainMenu.AddSeparator();
            mainMenu.AddLabel("Changelog:");
            mainMenu.AddLabel("v1.0.0 - Initial release");

            comboMenu = mainMenu.AddSubMenu("Combo Menu", "comboMenu");
            comboMenu.AddGroupLabel("Combo Menu");
            comboMenu.Add("useQ", new CheckBox("Use Q"));
            comboMenu.Add("useW", new CheckBox("Use W"));
            comboMenu.Add("useE", new CheckBox("Use E"));

            harassMenu = mainMenu.AddSubMenu("Harass Menu", "harassMenu");
            harassMenu.Add("useQ", new CheckBox("Use Q"));
            harassMenu.Add("useE", new CheckBox("Use E"));
            harassMenu.Add("useQToggle", new KeyBind("Q Toggle Harass", false, KeyBind.BindTypes.PressToggle, 'A'));

            ultimateMenu = mainMenu.AddSubMenu("Ultimate", "ultimateMenu");
            ultimateMenu.Add("useAutoUlt", new CheckBox("Use Auto-Ultimate"));
            ultimateMenu.Add("ultimateInterrupt", new CheckBox("Use (R) to Interrupt Spells"));
            ultimateMenu.AddSeparator();
            ultimateMenu.Add("minR", new Slider("Minimum enemies to cast (R)", 2, 1, 5));

            Interrupter.OnInterruptableSpell += OnInterruptableSpell;
            Game.OnTick += OnTick;
        }

        private static void OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (myHero.ChampionName != "Cassiopeia")
                return;

            if (ultimateMenu["ultimateInterruptt"].Cast<CheckBox>().CurrentValue)
            {
                if (sender.IsValidTarget(R.Range))
                {
                    if (R.IsReady())
                        R.Cast(sender);
                }
            }
        }

        private static void OnTick(EventArgs args)
        {
            if (myHero.ChampionName != "Cassiopeia")
                return;

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                var target = TargetSelector.GetTarget(850, DamageType.Magical);
                {
                    if (Q.IsReady() && comboMenu["useQ"].Cast<CheckBox>().CurrentValue && target.IsValidTarget(Q.Range))
                        Q.Cast(target);

                    if (E.IsReady() && comboMenu["useE"].Cast<CheckBox>().CurrentValue && target.IsValidTarget(E.Range) && target != null && target.IsVisible && !target.IsDead)
                    {
                        if ((target.HasBuffOfType(BuffType.Poison)))
                        {
                            if (target.IsValidTarget(E.Range))
                                E.Cast(target);
                        }
                    }

                    if (W.IsReady() && target.IsValidTarget(W.Range) && comboMenu["useW"].Cast<CheckBox>().CurrentValue && Environment.TickCount > LastQ + Q.CastDelay * 1000)
                        W.Cast(target);
                }
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                var target = TargetSelector.GetTarget(850, DamageType.Magical);
                {
                    if (E.IsReady() && harassMenu["useE"].Cast<CheckBox>().CurrentValue && target.IsValidTarget(E.Range) && target != null && target.IsVisible && !target.IsDead)
                    {
                        if ((target.HasBuffOfType(BuffType.Poison)))
                        {
                            if (target.IsValidTarget(E.Range))
                                E.Cast(target);
                        }
                    }

                    if (Q.IsReady() && harassMenu["useQ"].Cast<CheckBox>().CurrentValue && target.IsValidTarget(Q.Range))
                        Q.Cast(target);
                }
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                foreach (var minion in ObjectManager.Get<Obj_AI_Minion>().Where(x => E.IsInRange(x)
                    && !x.IsDead
                    && x.IsEnemy
                    && x.HasBuffOfType(BuffType.Poison)
                    && x.Health + 5 < Extensions.GetDamage(SpellSlot.E, x)))
                {
                    E.Cast(minion);
                }
            }

            OnUltimate();
            OnToggluseE();
        }

        public static void OnToggluseE()
        {
            var target = TargetSelector.GetTarget(850, DamageType.Magical);
            {
                if (Q.IsReady() && harassMenu["useQToggle"].Cast<KeyBind>().CurrentValue && target.IsValidTarget(Q.Range))
                    Q.Cast(target);
            }
        }

        public static void OnUltimate()
        {
            var target = TargetSelector.GetTarget(500, DamageType.Magical);
            var castPred = R.GetPrediction(target);
            {
                {
                    foreach (
                        var enemy in
                            ObjectManager.Get<AIHeroClient>()
                                .Where(enemy => enemy.Distance(myHero) <= Program.R.Range))
                    {
                        if (enemy.CountEnemiesInRange(500) >= ultimateMenu["minR"].Cast<Slider>().CurrentValue && ultimateMenu["useAutoUlt"].Cast<CheckBox>().CurrentValue && enemy.IsFacing(myHero)) //ObjectManager.Player
                            R.Cast(target.Position);
                    }
                }
            }
        }
    }
}
