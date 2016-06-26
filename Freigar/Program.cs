using System;
using System.Linq;
using EloBuddy.SDK.Enumerations;
using EloBuddy;
using EloBuddy.SDK.Events;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace Freigar
{

    class Program
    {
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        //Calls Player.Instance, this is the player
        public static AIHeroClient User = Player.Instance;

        private static Spell.Skillshot Q;

        private static Spell.Skillshot W;

        private static Spell.Skillshot E;

        private static Spell.Targeted R;

        private static Menu FreigMenu, ComboMenu, HarassMenu, LastHitMenu, LaneClearMenu, DrawMenu;

        private static void Loading_OnLoadingComplete(EventArgs args)
        {

            if (User.ChampionName != "Veigar")
            {
                return;
            }

            Q = new Spell.Skillshot(spellSlot: SpellSlot.Q, spellRange: 950, skillShotType: SkillShotType.Linear, castDelay: 250, spellSpeed: 1750, spellWidth: 70)
            { AllowedCollisionCount = 1 };

            W = new Spell.Skillshot(spellSlot: SpellSlot.W, spellRange: 900, skillShotType: SkillShotType.Linear, castDelay: 250, spellSpeed: null, spellWidth: 225)
            { AllowedCollisionCount = int.MaxValue };

            E = new Spell.Skillshot(spellSlot: SpellSlot.E, spellRange: 1125, skillShotType: SkillShotType.Circular, castDelay: 750, spellSpeed: null, spellWidth: 80);

            R = new Spell.Targeted(spellSlot: SpellSlot.R, spellRange: 650);

            FreigMenu = MainMenu.AddMenu("Freigar", "Freigar");

            ComboMenu = FreigMenu.AddSubMenu("Combo");

            ComboMenu.Add("Q", new CheckBox("Use Q"));
            ComboMenu.Add("CQMobile", new CheckBox("Use Q even if target is not stunned"));
            ComboMenu.Add("CQHitChance", new Slider("Q Hitchance for Combo: ", 3, 1, 5));
            ComboMenu.AddLabel("1 = Low 2 = Average 3 = Medium 4 = High 5 = Immobile");
            ComboMenu.Add("W", new CheckBox("Use W"));
            ComboMenu.Add("CWMobile", new CheckBox("Use W even if target is not stunned", false));
            ComboMenu.Add("ManaCW", new Slider("Block W at mana%: ", 5));
            ComboMenu.Add("E", new CheckBox("Use E"));
            ComboMenu.Add("CEHitChance", new Slider("E Hitchance for Combo: ", 4, 1, 5));
            ComboMenu.AddLabel("1 = Low 2 = Average 3 = Medium 4 = High 5 = Immobile");
            ComboMenu.Add("EMove", new Slider("E Effective Range:", 375, 350, 425));
            ComboMenu.AddLabel("How much adjustment for E placement. Higher will try to stun at longer range, but lower");
            ComboMenu.AddLabel("is more likely to stun.");
            ComboMenu.Add("R", new CheckBox("Use R"));
            ComboMenu.Add("RSafe", new Slider("Reduce R Calculation Damage by:", 5, 0, 50));
            ComboMenu.AddLabel("This is just for safety in case the calculations are too high and R doesn't kill the target");
            ComboMenu.AddLabel("(for pussies only)");


            HarassMenu = FreigMenu.AddSubMenu("Harass");

            HarassMenu.Add("HQ", new CheckBox("Use Q"));
            HarassMenu.Add("HQMobile", new CheckBox("Use Q even if target is not stunned"));
            HarassMenu.Add("HQHitChance", new Slider("Q Hitchance for Harass: ", 4, 1, 5));
            HarassMenu.AddLabel("1 = Low 2 = Average 3 = Medium 4 = High 5 = Immobile");
            HarassMenu.Add("HW", new CheckBox("Use W", false));
            HarassMenu.Add("HWMobile", new CheckBox("Use W even if target is not stunned", false));
            HarassMenu.Add("ManaHW", new Slider("Block W at mana%: ", 5));
            HarassMenu.Add("HE", new CheckBox("Use E", false));
            HarassMenu.Add("HEHitChance", new Slider("E Hitchance for Harass: ", 4, 1, 5));
            HarassMenu.AddLabel("1 = Low 2 = Average 3 = Medium 4 = High 5 = Immobile");
            HarassMenu.Add("HEMove", new Slider("E in Harass Effective Range:", 375, 350, 425));
            HarassMenu.AddLabel("How much adjustment for E placement. Higher will try to stun at longer range, but lower");
            HarassMenu.AddLabel("is more likely to stun.");

            LastHitMenu = FreigMenu.AddSubMenu("Q Stack");

            LastHitMenu.Add("LHQ", new CheckBox("Use Q to Last Hit"));
            LastHitMenu.Add("ManaLHQ", new Slider("Mana % for Last Hit Q: ", 30, 0, 100));
            LastHitMenu.Add("DoubleQ", new CheckBox("Only if Q kills two:", false));

            LaneClearMenu = FreigMenu.AddSubMenu("Lane Clear");

            LaneClearMenu.Add("LCQ", new CheckBox("Use Q to Clear", false));
            LaneClearMenu.Add("LCW", new CheckBox("Use W to Clear", false));
            LaneClearMenu.Add("LCNumW", new Slider("Only if W hits at least X enemy minions:", 4, 1, 5));

            DrawMenu = FreigMenu.AddSubMenu("Draws");

            DrawMenu.Add("usedraw", new CheckBox("Enable Drawings", true));
            DrawMenu.Add("IfReady", new CheckBox("Draw Only If Spell is Ready", false));
            DrawMenu.AddSeparator(1);
            DrawMenu.Add("drawQ", new CheckBox(" Draw Q", true));
            DrawMenu.Add("drawW", new CheckBox(" Draw W", false));
            DrawMenu.Add("drawE", new CheckBox(" Draw E", true));
            DrawMenu.Add("drawR", new CheckBox(" Draw R", false));
            DrawMenu.Add("DrawBar", new CheckBox("Draw Ready Damage on Healthbar:", true));

            if (DrawMenu["usedraw"].Cast<CheckBox>().CurrentValue)
                Drawing.OnDraw += Game_OnDraw;
            DrawMenu["usedraw"].Cast<CheckBox>().OnValueChange += (sender, vargs) =>
            {
                if (vargs.NewValue)
                    Drawing.OnDraw += Game_OnDraw;
                else
                    Drawing.OnDraw -= Game_OnDraw;
            };

            DamageIndicator.Initialize(Program.GetFullDmg);
            DamageIndicator.DrawingColor = System.Drawing.Color.LawnGreen;

            Game.OnTick += Game_OnTick;



        }



        private static void Game_OnTick(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.None))
            {
            }
            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }
            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.LaneClear))
            {
                LastHit();
            }
            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.LastHit))
            {
                LastHit();
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(1000, DamageType.Magical);

            if (target == null)
            {
                return;
            }

            if (ComboMenu["E"].Cast<CheckBox>().CurrentValue)
            {
                var Epred = E.GetPrediction(target);
                if (target.IsValidTarget(E.Range) && E.IsReady() && Epred.HitChance >= HitReturn((ComboMenu["CEHitChance"].Cast<Slider>().CurrentValue)) && !target.IsInvulnerable)
                {
                    if (target.CanMove)
                    {
                        var EPredN = Epred.CastPosition.Extend(User, ComboMenu["EMove"].Cast<Slider>().CurrentValue).To3D();
                        E.Cast(EPredN);
                    }
                    if (!target.CanMove)
                    {
                        var EPredCMN = target.ServerPosition.Extend(User, ComboMenu["EMove"].Cast<Slider>().CurrentValue).To3D();
                        E.Cast(EPredCMN);
                    }
                }
            }

            if (ComboMenu["W"].Cast<CheckBox>().CurrentValue)
            {
                if (ComboMenu["CWMobile"].Cast<CheckBox>().CurrentValue)
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
                else
                {
                    if (ComboMenu["ManaCW"].Cast<Slider>().CurrentValue <= Player.Instance.ManaPercent)
                    {
                        var Wpred = W.GetPrediction(target);
                        if (target.IsValidTarget(W.Range) && W.IsReady() && Wpred.HitChance >= HitChance.Medium &&
                            !target.IsInvulnerable && !target.CanMove)
                        {
                            W.Cast(target);
                        }
                    }
                }
            }


            if (ComboMenu["Q"].Cast<CheckBox>().CurrentValue)
            {
                if (ComboMenu["CQMobile"].Cast<CheckBox>().CurrentValue)
                {
                    var Qpred = Q.GetPrediction(target);
                    if (target.IsValidTarget(Q.Range) && Q.IsReady() && Qpred.HitChance >= HitReturn((ComboMenu["CQHitChance"].Cast<Slider>().CurrentValue)) && !target.IsInvulnerable)
                    {
                        Q.Cast(target);
                    }
                }
                else
                {
                    if (E.IsReady())
                    {
                        var Qpred = Q.GetPrediction(target);
                        if (target.IsValidTarget(Q.Range) && Q.IsReady() && Qpred.HitChance >= HitReturn((ComboMenu["CQHitChance"].Cast<Slider>().CurrentValue)) && !target.IsInvulnerable && !target.CanMove)
                        {
                            Q.Cast(target);
                        }
                    }
                    else if (!E.IsReady())
                    {
                        var Qpred = Q.GetPrediction(target);
                        if (target.IsValidTarget(Q.Range) && Q.IsReady() && Qpred.HitChance >= HitReturn((ComboMenu["CQHitChance"].Cast<Slider>().CurrentValue)) && !target.IsInvulnerable)
                        {
                            Q.Cast(target);
                        }
                    }
                }
            }

            if (ComboMenu["R"].Cast<CheckBox>().CurrentValue)
            {
                if (target.IsValidTarget(R.Range) && R.IsReady() && !target.IsInvulnerable)
                {
                    float rcalc = RCalc(target);
                    if (rcalc - ComboMenu["RSafe"].Cast<Slider>().CurrentValue >= target.TotalShieldHealth())
                        R.Cast(target);
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(1000, DamageType.Magical);
            if (target == null)
            {
                return;
            }

            if (HarassMenu["HE"].Cast<CheckBox>().CurrentValue)
            {
                var Epred = E.GetPrediction(target);
                if (target.IsValidTarget(E.Range) && E.IsReady() && Epred.HitChance >= HitReturn((HarassMenu["HEHitChance"].Cast<Slider>().CurrentValue)) && !target.IsInvulnerable)
                {
                    if (target.CanMove)
                    {
                        var EPredN = Epred.CastPosition.Extend(User, HarassMenu["HEMove"].Cast<Slider>().CurrentValue).To3D();
                        E.Cast(EPredN);
                    }
                    if (!target.CanMove)
                    {
                        var EPredCMN = target.ServerPosition.Extend(User, HarassMenu["HEMove"].Cast<Slider>().CurrentValue).To3D();
                        E.Cast(EPredCMN);
                    }
                }
            }

            if (HarassMenu["HW"].Cast<CheckBox>().CurrentValue)
                if (HarassMenu["HWMobile"].Cast<CheckBox>().CurrentValue)
                {
                    if (HarassMenu["ManaHW"].Cast<Slider>().CurrentValue <= Player.Instance.ManaPercent)
                    {
                        var Wpred = W.GetPrediction(target);
                        if (target.IsValidTarget(W.Range) && W.IsReady() && Wpred.HitChance >= HitChance.High && !target.IsInvulnerable)
                        {
                            W.Cast(target);
                        }
                    }
                }
                else
                {
                    if (HarassMenu["ManaHW"].Cast<Slider>().CurrentValue <= Player.Instance.ManaPercent)
                    {
                        var Wpred = W.GetPrediction(target);
                        if (target.IsValidTarget(W.Range) && W.IsReady() && Wpred.HitChance >= HitChance.Medium && !target.IsInvulnerable && !target.CanMove)
                        {
                            W.Cast(target);
                        }
                    }
                }

            if (HarassMenu["HQ"].Cast<CheckBox>().CurrentValue)
            {
                if (HarassMenu["HQMobile"].Cast<CheckBox>().CurrentValue)
                {
                    var Qpred = Q.GetPrediction(target);
                    if (target.IsValidTarget(Q.Range) && Q.IsReady() && Qpred.HitChance >= HitReturn((HarassMenu["HQHitChance"].Cast<Slider>().CurrentValue)) && !target.IsInvulnerable)
                    {
                        Q.Cast(target);
                    }
                }
                else
                {
                    if (E.IsReady())
                    {
                        var Qpred = Q.GetPrediction(target);
                        if (target.IsValidTarget(Q.Range) && Q.IsReady() && Qpred.HitChance >= HitReturn((HarassMenu["HQHitChance"].Cast<Slider>().CurrentValue)) && !target.IsInvulnerable && !target.CanMove)
                        {
                            Q.Cast(target);
                        }
                    }
                    else if (!E.IsReady())
                    {
                        var Qpred = Q.GetPrediction(target);
                        if (target.IsValidTarget(Q.Range) && Q.IsReady() && Qpred.HitChance >= HitReturn((HarassMenu["HQHitChance"].Cast<Slider>().CurrentValue)) && !target.IsInvulnerable)
                        {
                            Q.Cast(target);
                        }
                    }
                }
            }
        }


        private static void LastHit()
        {
            if (LastHitMenu["LHQ"].Cast<CheckBox>().CurrentValue)
                if (LastHitMenu["ManaLHQ"].Cast<Slider>().CurrentValue <= Player.Instance.ManaPercent)
                {
                    if (LastHitMenu["DoubleQ"].Cast<CheckBox>().CurrentValue)
                    {
                        var source = EntityManager.MinionsAndMonsters.EnemyMinions.FirstOrDefault(m => m.IsValidTarget(Q.Range) && QCalc(m) > m.TotalShieldHealth() &&
                         m.IsEnemy && !m.IsDead && m.IsValid && !m.IsInvulnerable);
                        var Dsource = EntityManager.MinionsAndMonsters.EnemyMinions.FirstOrDefault(m => m.IsValidTarget(Q.Range) && QCalc(m) > m.TotalShieldHealth() &&
                         m.IsEnemy && !m.IsDead && m.IsValid && !m.IsInvulnerable && m != source && User.Distance(m) > User.Distance(source));
                        if (source == null)
                        {
                            return;
                        }
                        if (Dsource == null)
                        {
                            return;
                        }
                        var Qpred = Q.GetPrediction(source);
                        var DQpred = Q.GetPrediction(Dsource);
                        var Dbl = DQpred.GetCollisionObjects<Obj_AI_Minion>();
                        if (Dbl.Contains(source))
                        {
                            Q.Cast(Dsource);
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        var source = EntityManager.MinionsAndMonsters.EnemyMinions.FirstOrDefault(m => m.IsValidTarget(Q.Range) && QCalc(m) > m.TotalShieldHealth() &&
                        m.IsEnemy && !m.IsDead && m.IsValid && !m.IsInvulnerable);
                        if (source == null)
                        {
                            return;
                        }
                        var Qpred = Q.GetPrediction(source);
                        if (Qpred.HitChance > HitChance.Collision)
                        {
                            Q.Cast(source);
                        }
                        else
                        {
                            return;
                        }

                    }
                }
        }

        private static void LaneClear()
        {
            if (LaneClearMenu["LCQ"].Cast<CheckBox>().CurrentValue)
            {
                var source = EntityManager.MinionsAndMonsters.EnemyMinions.FirstOrDefault(m => m.IsValidTarget(Q.Range) && m.IsEnemy && !m.IsDead && m.IsValid && !m.IsInvulnerable);
                var Dsource = EntityManager.MinionsAndMonsters.EnemyMinions.FirstOrDefault(m => m.IsValidTarget(Q.Range) && m.IsEnemy && !m.IsDead && m.IsValid && !m.IsInvulnerable && m != source && User.Distance(m) > User.Distance(source));
                if (source == null)
                {
                    return;
                }
                if (Dsource == null)
                {
                    return;
                }
                var Qpred = Q.GetPrediction(source);
                var DQpred = Q.GetPrediction(Dsource);
                var Dbl = DQpred.GetCollisionObjects<Obj_AI_Minion>();
                if (Dbl.Contains(source))
                {
                    Q.Cast(Dsource);
                }
                else
                {
                    return;
                }
            }
            if (LaneClearMenu["LCW"].Cast<CheckBox>().CurrentValue)
            {
                var source = EntityManager.MinionsAndMonsters.EnemyMinions.FirstOrDefault(m => m.IsValidTarget(W.Range) && m.IsEnemy && !m.IsDead && m.IsValid && !m.IsInvulnerable);
                if (source == null)
                {
                    return;
                }

                var Wpred = W.GetPrediction(source);
                //var LocW = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,User.ServerPosition,
                // 700.0f, false), 225.0f, 900, 1500, int.MaxValue, User.ServerPosition.To2D());
                var nearFLW = EntityManager.MinionsAndMonsters.EnemyMinions.Where(t => t.IsValidTarget(W.Range) && t.Distance(source) < 112.5f);
                var ANFLW = nearFLW.ToArray();
                if (ANFLW.Length >= LaneClearMenu["LCNumW"].Cast<Slider>().CurrentValue)
                {
                    W.Cast(source);
                }

                else
                {
                    return;
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
            var _Targ = m; return (QLevelInd() + QMDmg()) * MagReturn(_Targ);
        }
        private static float QCalc(AIHeroClient target)
        {
            var _Targ = target;
            if (_Targ.SpellBlock <= 0)
            {
                return (QLevelInd() + QMDmg()) * MagReturnNeg(_Targ);
            }
            else if (_Targ.SpellBlock >= 0)
            {
                return (QLevelInd() + QMDmg()) * MagReturn(_Targ);
            }
            return 0;
        }
        private static float QLevelInd()
        {
            if (Program.Q.Level == 0)
                return 0f;
            if (Program.Q.Level == 1)
                return 75.0f;
            if (Program.Q.Level == 2)
                return 110.0f;
            if (Program.Q.Level == 3)
                return 150.0f;
            if (Program.Q.Level == 4)
                return 190.0f;
            if (Program.Q.Level == 5)
                return 230.0f;
            else
                return 0f;
        }
        private static float QMDmg()
        {
            return (0.60f * User.TotalMagicalDamage);
        }

        private static float WCalc(AIHeroClient target)
        {
            var _Targ = target;
            if (_Targ.SpellBlock <= 0)
            {
                return (WLevelInd() + WMDmg()) * MagReturnNeg(_Targ);
            }
            else if (_Targ.SpellBlock >= 0)
            {
                return (WLevelInd() + WMDmg()) * MagReturn(_Targ);
            }
            return 0;
        }

        private static float WLevelInd()
        {
            if (Program.W.Level == 0)
                return 0f;
            if (Program.W.Level == 1)
                return 100.0f;
            if (Program.W.Level == 2)
                return 150.0f;
            if (Program.W.Level == 3)
                return 200.0f;
            if (Program.W.Level == 4)
                return 250.0f;
            if (Program.W.Level == 5)
                return 300.0f;
            else
                return 0f;
        }

        private static float WMDmg()
        {
            return (1.0f * User.TotalMagicalDamage);
        }

        private static float RCalc(AIHeroClient target)
        {
            var _Targ = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            if (_Targ == null)
            {
                return 0;
            }
            if (_Targ.SpellBlock <= 0)
            {
                return ((RLevelInd() + RMDmg()) * RHPPercent(_Targ)) * MagReturnNeg(_Targ);
            }
            if (_Targ.SpellBlock >= 0)
            {

                return ((RLevelInd() + RMDmg()) * RHPPercent(_Targ)) * MagReturn(_Targ);
            }

            return 0;
        }
        private static float RHPPercent(AIHeroClient target)
        {
            var _Targ = target;

            return ((100.0f - _Targ.HealthPercent) * 1.5f * 0.01f + 1.0f);
        }

        private static float RMDmg()
        {
            return (0.75f * User.TotalMagicalDamage);
        }

        private static float RLevelInd()
        {
            if (Program.R.Level == 0)
                return 0f;
            if (Program.R.Level == 1)
                return 175.0f;
            if (Program.R.Level == 2)
                return 250.0f;
            if (Program.R.Level == 3)
                return 325.0f;
            else
                return 0f;
        }

        private static float QFCalc(AIHeroClient target)
        {
            var _Targ = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            if (_Targ == null)
            {
                return 0;
            }
            if (!Q.IsReady())
                return 0;
            if (_Targ.SpellBlock <= 0)
            {
                return (QLevelInd() + QMDmg()) * MagReturnNeg(_Targ);
            }
            else if (_Targ.SpellBlock >= 0)
            {
                return (QLevelInd() + QMDmg()) * MagReturn(_Targ);
            }
            else
            {
                return 0;
            }
        }

        private static float WFCalc(AIHeroClient target)
        {
            var _Targ = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            if (_Targ == null)
            {
                return 0;
            }
            if (!W.IsReady())
                return 0;
            if (_Targ.SpellBlock <= 0)
            {
                return (WLevelInd() + WMDmg()) * MagReturnNeg(_Targ);
            }
            else if (_Targ.SpellBlock >= 0)
            {
                return (WLevelInd() + WMDmg()) * MagReturn(_Targ);
            }
            else
            {
                return 0;
            }
        }
        private static float RFCalc(AIHeroClient target)
        {
            var _Targ = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            if (_Targ == null)
            {
                return 0;
            }
            if (!R.IsReady())
                return 0;
            if (_Targ.SpellBlock <= 0)
            {
                return ((RLevelInd() + RMDmg()) * RHPPercent(_Targ)) * MagReturnNeg(_Targ);
            }
            if (_Targ.SpellBlock >= 0)
            {

                return ((RLevelInd() + RMDmg()) * RHPPercent(_Targ)) * MagReturn(_Targ);
            }
            else
                return 0;
        }

        public static float GetFullDmg(AIHeroClient target)
        {
            var _Targ = target;
            return QFCalc(_Targ) + WFCalc(_Targ) + RFCalc(_Targ);
        }

        private static float MagReturn(AIHeroClient target)
        {
            var _Targ = target;
            if ((_Targ.SpellBlock - PenReturn()) < 0)
            {
                return 1;
            }
            else
            {
                if (Item.HasItem(3135, User))
                    if ((100.0f / (100.0f + (_Targ.SpellBlock * 0.65f - PenReturn()))) > 1)
                        return 1;
                    else
                        return (100.0f / (100.0f + (_Targ.SpellBlock * 0.65f - PenReturn())));
                else
                    return (100.0f / (100.0f + (_Targ.SpellBlock - PenReturn())));
            }
        }
        private static float MagReturnNeg(AIHeroClient target)
        {
            return (2.0f - (100.0f / (100.0f + target.SpellBlock)));
        }
        private static float PenReturn()
        {
            var Pen = 0.0f;
            if (Item.HasItem(3151, User))
                Pen = Pen + 15;
            if (Item.HasItem(3136, User))
                if (!Item.HasItem(3151, User))
                    Pen = Pen + 15;
            if (Item.HasItem(3020, User))
                Pen = Pen + 15;
            if (!(Item.HasItem(3151, User)) && !(Item.HasItem(3136, User)) && !(Item.HasItem(3020, User)))
            {
                return 0;
            }
            return Pen;
        }

        private static float MagReturn(Obj_AI_Minion target)
        {
            return ((100.0f / (100.0f + target.SpellBlock)));
        }
        private static float MagReturnNeg(Obj_AI_Minion target)
        {
            return (2.0f - (100.0f / (100.0f + target.SpellBlock)));
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
                            Circle.Draw(Color.Blue, Q.Range, Player.Instance.Position);
                        }
                    }
                    if (DrawMenu["drawW"].Cast<CheckBox>().CurrentValue)
                    {
                        if (W.IsReady())
                        {
                            Circle.Draw(Color.Purple, W.Range, Player.Instance.Position);
                        }
                    }
                    if (DrawMenu["drawE"].Cast<CheckBox>().CurrentValue)
                    {
                        if (E.IsReady())
                        {
                            Circle.Draw(Color.Gray, E.Range, Player.Instance.Position);
                        }
                    }
                    if (DrawMenu["drawR"].Cast<CheckBox>().CurrentValue)
                    {
                        if (R.IsReady())
                        {
                            Circle.Draw(Color.Blue, R.Range, Player.Instance.Position);
                        }
                    }
                    DamageIndicator.HealthbarEnabled = DrawMenu["drawBar"].Cast<CheckBox>().CurrentValue;
                    DamageIndicator.PercentEnabled = DrawMenu["drawHP"].Cast<CheckBox>().CurrentValue;

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
                        Circle.Draw(Color.Blue, Q.Range, Player.Instance.Position);
                    }
                    if (DrawMenu["drawW"].Cast<CheckBox>().CurrentValue)
                    {
                        Circle.Draw(Color.Purple, W.Range, Player.Instance.Position);
                    }
                    if (DrawMenu["drawE"].Cast<CheckBox>().CurrentValue)
                    {
                        Circle.Draw(Color.DarkGray, 700, Player.Instance.Position);
                    }
                    if (DrawMenu["drawR"].Cast<CheckBox>().CurrentValue)
                    {
                        Circle.Draw(Color.Blue, R.Range, Player.Instance.Position);
                    }
                    DamageIndicator.HealthbarEnabled = DrawMenu["drawBar"].Cast<CheckBox>().CurrentValue;
                    DamageIndicator.PercentEnabled = DrawMenu["drawHP"].Cast<CheckBox>().CurrentValue;
                }
                catch (Exception)
                {

                }
            }

        }

    }
}