using Modding;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using UnityEngine;
using Vasi;

namespace Sanic
{
    [UsedImplicitly]
    public class Sanic : Mod, IGlobalSettings<GlobalModSettings>, ITogglableMod
    {
        public void OnLoadGlobal(GlobalModSettings s) => _globalSettings = s;

        public GlobalModSettings OnSaveGlobal() => _globalSettings;

        private GlobalModSettings _globalSettings = new();

        public override string GetVersion() => VersionUtil.GetVersion<Sanic>();

        private static readonly MethodInfo[] FreezeCoroutines = (
            from method in typeof(GameManager).GetMethods()
            where method.Name.StartsWith("FreezeMoment")
            where method.ReturnType == typeof(IEnumerator)
            select method.GetCustomAttribute<IteratorStateMachineAttribute>() into attr
            select attr.StateMachineType into type
            select type.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance)
        ).ToArray();

        private ILHook[] _coroutineHooks;

        public override void Initialize()
        {
            Time.timeScale = _globalSettings.SpeedMultiplier;

            ModHooks.HeroUpdateHook += Update;

            On.GameManager.SetTimeScale_float += GameManager_SetTimeScale_1;
            On.QuitToMenu.Start += QuitToMenu_Start;

            _coroutineHooks = new ILHook[FreezeCoroutines.Length];

            foreach ((MethodInfo coro, int idx) in FreezeCoroutines.Select((mi, idx) => (mi, idx)))
            {
                _coroutineHooks[idx] = new ILHook(coro, ScaleFreeze);

                LogDebug($"Hooked {coro.DeclaringType?.Name}!");
            }
        }

        public void Unload()
        {
            foreach (ILHook hook in _coroutineHooks)
                hook.Dispose();

            Time.timeScale = 1;

            ModHooks.HeroUpdateHook -= Update;

            On.GameManager.SetTimeScale_float -= GameManager_SetTimeScale_1;
            On.QuitToMenu.Start -= QuitToMenu_Start;
        }


        private void ScaleFreeze(ILContext il)
        {
            var cursor = new ILCursor(il);

            cursor.GotoNext
            (
                MoveType.After,
                x => x.MatchLdfld(out _),
                x => x.MatchCall<Time>("get_unscaledDeltaTime")
            );

            cursor.EmitDelegate<Func<float>>(() => _globalSettings.SpeedMultiplier);

            cursor.Emit(OpCodes.Mul);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                if (Math.Abs(Time.timeScale - _globalSettings.SpeedMultiplier) < Mathf.Epsilon)
                    Time.timeScale += 0.05f;

                _globalSettings.SpeedMultiplier += 0.05f;
            }

            // ReSharper disable once InvertIf
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if (Math.Abs(Time.timeScale - _globalSettings.SpeedMultiplier) < Mathf.Epsilon)
                    Time.timeScale -= 0.05f;

                _globalSettings.SpeedMultiplier -= 0.05f;
            }
        }

        private IEnumerator QuitToMenu_Start(On.QuitToMenu.orig_Start orig, QuitToMenu self)
        {
            yield return orig(self);

            TimeController.GenericTimeScale = _globalSettings.SpeedMultiplier;
        }

        private void GameManager_SetTimeScale_1(On.GameManager.orig_SetTimeScale_float orig, GameManager self, float newTimeScale)
        {
            if (Mirror.GetField<GameManager, int>(self, "timeSlowedCount") > 1)
                newTimeScale = Math.Min(newTimeScale, TimeController.GenericTimeScale);
            
            TimeController.GenericTimeScale = (newTimeScale <= 0.01f ? 0f : newTimeScale) * _globalSettings.SpeedMultiplier;
        }
    }

    public class GlobalModSettings
    {
        public float SpeedMultiplier = 1.3f;
    }
}