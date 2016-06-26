using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy.SDK.Enumerations;
using EloBuddy;
using EloBuddy.SDK.Events;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using Microsoft.Win32;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace BSEzreal
{

    class Program
    {
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        //Calls Player.Instance, this is the player
        private static AIHeroClient User = Player.Instance;

        private static Spell.Skillshot Q;

        private static Spell.Skillshot W;

        private static Spell.Skillshot E;

        private static Spell.Skillshot R;

        private static Menu EzrealMenu, ComboMenu, AutoHarassMenu, HarassMenu, LastHitMenu, DrawMenu;


        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (User.ChampionName != "Ezreal")
            {
                return;
            }

            Q = new Spell.Skillshot(spellSlot: SpellSlot.Q, spellRange: 1150, skillShotType: SkillShotType.Linear, castDelay: 250, spellSpeed: 2000, spellWidth: 60)
            { AllowedCollisionCount = 0 };

            W = new Spell.Skillshot(spellSlot: SpellSlot.W, spellRange: 1000, skillShotType: SkillShotType.Linear, castDelay: 250, spellSpeed: 1550, spellWidth: 80)
            { AllowedCollisionCount = int.MaxValue };

            E = new Spell.Skillshot(spellSlot: SpellSlot.E, spellRange: 475, skillShotType: SkillShotType.Circular, castDelay: 250, spellSpeed: null, spellWidth: 750);

            R = new Spell.Skillshot(spellSlot: SpellSlot.R, spellRange: 3000, skillShotType: SkillShotType.Linear, castDelay: 1000, spellSpeed: 2000, spellWidth: 160)
            { AllowedCollisionCount = int.MaxValue };

            EzrealMenu = MainMenu.AddMenu("BSEzreal", "BSEzreal");

            ComboMenu = EzrealMenu.AddSubMenu("Combo");

            ComboMenu.Add("Q", new CheckBox("Use Q"));
            ComboMenu.Add("W", new CheckBox("Use W"));
            ComboMenu.Add("E", new CheckBox("Use E"));
            ComboMenu.Add("R", new CheckBox("Use R"));
            ComboMenu.Add("CQHitChance", new Slider("Q Hitchance for Combo: ", 3, 1, 5));
            ComboMenu.AddLabel("1 = Low 2 = Average 3 = Medium 4 = High 5 = Immobile");
            ComboMenu.Add("ManaCW", new Slider("Block W if Mana % is Below: ", 30, 0, 100));

            AutoHarassMenu = EzrealMenu.AddSubMenu("Auto Harass");

            AutoHarassMenu.Add("AQ", new CheckBox("Auto Q"));
            AutoHarassMenu.AddSeparator();
            AutoHarassMenu.Add("PriorityAutoH", new Slider("Use Auto Q only if Target Selector is >=: ", 4, 0, 5));
            AutoHarassMenu.Add("ManaAQ", new Slider("Mana % for Auto Q: ", 60, 0, 100));
            AutoHarassMenu.Add("AQHitChance", new Slider("Q Hitchance for Auto Harass: ", 4, 1, 5));
            AutoHarassMenu.AddLabel("1 = Low 2 = Average 3 = Medium 4 = High 5 = Immobile");

            HarassMenu = EzrealMenu.AddSubMenu("Harass");

            HarassMenu.Add("HQ", new CheckBox("Use Q"));
            HarassMenu.Add("HW", new CheckBox("Use W"));
            HarassMenu.Add("HQHitChance", new Slider("Q Hitchance for Harass: ", 4, 1, 5));
            HarassMenu.AddLabel("1 = Low 2 = Average 3 = Medium 4 = High 5 = Immobile");
            HarassMenu.Add("ManaHW", new Slider("Block W if Mana % is Below: ", 40, 0, 100));

            LastHitMenu = EzrealMenu.AddSubMenu("Farm");

            LastHitMenu.Add("LHQ", new CheckBox("Use Q to Last Hit"));
            LastHitMenu.Add("ManaLHQ", new Slider("Mana % for Last Hit Q: ", 70, 0, 100));
            LastHitMenu.Add("CanAutoLHQ", new CheckBox("Only if Orbwalker cannot kill minion"));

            DrawMenu = EzrealMenu.AddSubMenu("Draws");

            DrawMenu.Add("usedraw", new CheckBox("Enable Drawings", true));
            DrawMenu.Add("IfReady", new CheckBox("Draw Only If Spell is Ready", true));
            DrawMenu.AddSeparator(1);
            DrawMenu.Add("drawQ", new CheckBox(" Draw Q", true));
            DrawMenu.Add("drawW", new CheckBox(" Draw W", true));
            DrawMenu.Add("drawR", new CheckBox(" Draw R", false));

            if (DrawMenu["usedraw"].Cast<CheckBox>().CurrentValue)
                Drawing.OnDraw += Game_OnDraw;
            DrawMenu["usedraw"].Cast<CheckBox>().OnValueChange += (sender, vargs) =>
            {
                if (vargs.NewValue)
                    Drawing.OnDraw += Game_OnDraw;
                else
                    Drawing.OnDraw -= Game_OnDraw;
            };

            Game.OnTick += Game_OnTick;

        }

        private static void Game_OnTick(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None))
            {
                AutoHarass();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                LastHit();
                AutoHarass();
            }
            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.LastHit))
            {
                LastHit();
            }

        }
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

            if (target == null)
            {
                return;
            }

            if (ComboMenu["Q"].Cast<CheckBox>().CurrentValue)
            {
                var Qpred = Q.GetPrediction(target);

                if (target.IsValidTarget(Q.Range) && Q.IsReady() && Qpred.HitChance >= HitReturn((ComboMenu["CQHitChance"].Cast<Slider>().CurrentValue)) && !target.IsInvulnerable)
                {
                    Q.Cast(target);
                }
            }

            if (ComboMenu["W"].Cast<CheckBox>().CurrentValue)
            {
                if (ComboMenu["ManaCW"].Cast<Slider>().CurrentValue <= Player.Instance.ManaPercent)
                {
                    var Wpred = W.GetPrediction(target);
                    if (target.IsValidTarget(W.Range) && W.IsReady() && Wpred.HitChance >= HitChance.High &&
                        !target.IsInvulnerable)
                    {
                        W.Cast(target);
                    }
                }
            }

            if (ComboMenu["E"].Cast<CheckBox>().CurrentValue)
            {
                var Epred = E.GetPrediction(target);

                if (target.IsValidTarget(E.Range) && E.IsReady() && Epred.HitChance >= HitChance.High && !target.IsInvulnerable)
                {
                    E.Cast(target);
                }
            }

            if (ComboMenu["R"].Cast<CheckBox>().CurrentValue)
            {
                float rcalc = RCalc(target);
                var Rpred = R.GetPrediction(target);
                var RQpred = Q.GetPrediction(target);

                if (target.IsValidTarget(2150) && R.IsReady() && Rpred.HitChance >= HitChance.Medium && !target.IsInvulnerable)
                {
                    if (rcalc >= target.Health && !target.IsValidTarget(550) && RQpred.HitChance >= HitChance.Collision)
                        R.Cast(target);
                }
            }
        }

        private static void AutoHarass()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
            if (target == null)
            {
                return;
            }

                if (AutoHarassMenu["ManaAQ"].Cast<Slider>().CurrentValue <= Player.Instance.ManaPercent)
                {
                    var Qpred = Q.GetPrediction(target);
                if (target.IsValidTarget(1100) && Q.IsReady() && Qpred.HitChance >= HitReturn((AutoHarassMenu["AQHitChance"].Cast<Slider>().CurrentValue)) && !target.IsInvulnerable && 
                    TargetSelector.GetPriority(target) >= AutoHarassMenu["PriorityAutoH"].Cast<Slider>().CurrentValue)
                    {
                        Q.Cast(target);
                    }
                }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
            if (target == null)
            {
                return;
            }
            if (HarassMenu["HQ"].Cast<CheckBox>().CurrentValue)
            {
                var Qpred = Q.GetPrediction(target);

                if (target.IsValidTarget(Q.Range) && Q.IsReady() && Qpred.HitChance >= HitReturn((HarassMenu["HQHitChance"].Cast<Slider>().CurrentValue)) && !target.IsInvulnerable)
                {
                    Q.Cast(target);
                }
            }

            if (HarassMenu["HW"].Cast<CheckBox>().CurrentValue)
            {
                if (HarassMenu["ManaHW"].Cast<Slider>().CurrentValue <= Player.Instance.ManaPercent)
                {
                     var Wpred = W.GetPrediction(target);
                     if (target.IsValidTarget(W.Range) && W.IsReady() && Wpred.HitChance >= HitChance.High &&
                         !target.IsInvulnerable)
                        {
                            W.Cast(target);
                        }
                }
            }
        }

        private static void LastHit()
        {
            if (LastHitMenu["CanAutoLHQ"].Cast<CheckBox>().CurrentValue)
            {
                if (LastHitMenu["LHQ"].Cast<CheckBox>().CurrentValue)
                {
                    if (LastHitMenu["ManaLHQ"].Cast<Slider>().CurrentValue <= Player.Instance.ManaPercent)
                    {
                        var source = EntityManager.MinionsAndMonsters.EnemyMinions.FirstOrDefault(m => m.IsValidTarget(Q.Range) && QCalc(m) > m.TotalShieldHealth() &&
                        m.IsEnemy && !m.IsDead && m.IsValid && !m.IsInvulnerable && !m.IsValidTarget(500));
                        if (source == null)
                        {
                            return;
                        }
                        Q.Cast(source);

                    }
                }
            }

            else
            {

                if (LastHitMenu["LHQ"].Cast<CheckBox>().CurrentValue)
                {
                    if (LastHitMenu["ManaLHQ"].Cast<Slider>().CurrentValue <= Player.Instance.ManaPercent)
                    {
                        var source = EntityManager.MinionsAndMonsters.EnemyMinions.FirstOrDefault(m => m.IsValidTarget(Q.Range) && QCalc(m) > m.TotalShieldHealth() &&
                        m.IsEnemy && !m.IsDead && m.IsValid && !m.IsInvulnerable);
                        if (source == null)
                        {
                            return;
                        }
                        Q.Cast(source);
                    }
                }
            }
        }

        private static HitChance HitReturn(int slide)
        {
                if (slide == 1)
                return HitChance.Low;
            else if (slide == 2)
                return HitChance.AveragePoint;
            else if (slide == 3)
                return HitChance.Medium;
            else if (slide == 4)
                return HitChance.High;
            else if (slide == 5)
                return HitChance.Immobile;
            else
                 return HitChance.High;
        }

        private static float QCalc(Obj_AI_Minion m)
        {
            var _Targ = m;
            return User.CalculateDamageOnUnit(m, DamageType.Physical,
                (float)(new[] { 0, 35, 55, 75, 95, 115 }[Program.Q.Level] + 1.1f * User.FlatPhysicalDamageMod + 0.4f * User.FlatMagicDamageMod * (100.0f / (100.0f + _Targ.Armor))));
        }
        private static float RCalc(AIHeroClient target)
        {
            var _Targ = TargetSelector.GetTarget(2150, DamageType.Magical);

            if (_Targ.SpellBlock <= 0)
            {
                return User.CalculateDamageOnUnit(target, DamageType.Magical,
                (float)(new[] { 0, 350, 500, 650 }[Program.R.Level] + 1.0f * User.FlatPhysicalDamageMod + 0.9f * User.FlatMagicDamageMod * (2.0f - (100.0f / (100.0f + _Targ.SpellBlock)))));
            }
            else if (_Targ.SpellBlock >= 0)
            {
                return User.CalculateDamageOnUnit(target, DamageType.Magical,
                (float)(new[] { 0, 350, 500, 650 }[Program.R.Level] + 1.0f * User.FlatPhysicalDamageMod + 0.9f * User.FlatMagicDamageMod * (100.0f / (100.0f + _Targ.SpellBlock))));
            }
            else
            {
                return User.CalculateDamageOnUnit(target, DamageType.Magical,
                (float)(new[] { 0, 350, 500, 650 }[Program.R.Level] + 1.0f * User.FlatPhysicalDamageMod + 0.9f * User.FlatMagicDamageMod * (100.0f / (100.0f + _Targ.SpellBlock))));
            }
        }
        private static void Game_OnDraw(EventArgs args)
        {
            if (DrawMenu["IfReady"].Cast<CheckBox>().CurrentValue)
            {
                try
                {
                    if (DrawMenu["drawQ"].Cast<CheckBox>().CurrentValue)
                    {
                        if (Q.IsReady())
                        {
                            Circle.Draw(Color.Green, Q.Range, Player.Instance.Position);
                        }
                    }
                    if (DrawMenu["drawW"].Cast<CheckBox>().CurrentValue)
                    {
                        if (W.IsReady())
                        {
                            Circle.Draw(Color.Yellow, W.Range, Player.Instance.Position);
                        }
                    }
                    if (DrawMenu["drawR"].Cast<CheckBox>().CurrentValue)
                    {
                        if (R.IsReady())
                        {
                            Circle.Draw(Color.White, R.Range, Player.Instance.Position);
                        }
                    }

                }
                catch (Exception)
                {

                }
            }
            else
            {
                try
                {
                    if (DrawMenu["drawQ"].Cast<CheckBox>().CurrentValue)
                    {
                            Circle.Draw(Color.Green, Q.Range, Player.Instance.Position);
                    }
                    if (DrawMenu["drawW"].Cast<CheckBox>().CurrentValue)
                    {
                            Circle.Draw(Color.Yellow, W.Range, Player.Instance.Position);
                    }
                    if (DrawMenu["drawR"].Cast<CheckBox>().CurrentValue)
                    {
                            Circle.Draw(Color.Red, R.Range, Player.Instance.Position);
                    }
                    if (DrawMenu["drawXR"].Cast<CheckBox>().CurrentValue)
                    {
                            Circle.Draw(Color.Red, 700, Player.Instance.Position);
                    }
                }
                catch (Exception)
                {

                }
            }   

        }
    }
}
