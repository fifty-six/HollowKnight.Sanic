using Modding;
using On;
using Sanic;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Sanic : Mod<SaveSettings, GlobalModSettings>
{
        public readonly FieldInfo timeSlowedField;

        internal static Sanic.Sanic Instance;

        public override string GetVersion()
        {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public Sanic()
        {
                Instance = this;
                base.GlobalSettings.SpeedMultiplier = base.GlobalSettings.SpeedMultiplier;
                SaveGlobalSettings();
                timeSlowedField = typeof(GameManager).GetField("timeSlowed", BindingFlags.Instance | BindingFlags.NonPublic);
                Time.timeScale = base.GlobalSettings.SpeedMultiplier;
                ModHooks.Instance.HeroUpdateHook += Instance_HeroUpdateHook;
                On.GameManager.SetTimeScale_float += GameManager_SetTimeScale_1;
                On.GameManager.FreezeMoment_float_float_float_float += GameManager_FreezeMoment_1;
                On.QuitToMenu.Start += QuitToMenu_Start;
        }

        private void Instance_HeroUpdateHook()
        {
                if (Input.GetKeyDown(KeyCode.KeypadPlus))
                {
                        if (Time.timeScale == base.GlobalSettings.SpeedMultiplier)
                        {
                                Time.timeScale += 0.05f;
                        }

                        base.GlobalSettings.SpeedMultiplier += 0.05f;

                        Log(Time.timeScale);
                        Log(base.GlobalSettings.SpeedMultiplier);

                        SaveGlobalSettings();
                }

                if (Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                        if (Time.timeScale == base.GlobalSettings.SpeedMultiplier)
                        {
                                Time.timeScale -= 0.05f;
                        }

                        base.GlobalSettings.SpeedMultiplier -= 0.05f;

                        Log(Time.timeScale);

                        SaveGlobalSettings();
                }
        }

        private IEnumerator GameManager_FreezeMoment_1(On.GameManager.orig_FreezeMoment_float_float_float_float orig, GameManager self, float rampDownTime, float waitTime, float rampUpTime, float targetSpeed)
        {
                if (!(bool)timeSlowedField.GetValue(self))
                {
                        timeSlowedField.SetValue(self, true);
                        yield return self.StartCoroutine(SetTimeScale(targetSpeed, rampDownTime));
                        for (float timer = 0f; timer < waitTime; timer += Time.unscaledDeltaTime * base.GlobalSettings.SpeedMultiplier)
                        {
                                yield return null;
                        }
                        yield return self.StartCoroutine(SetTimeScale(1f, rampUpTime));
                        timeSlowedField.SetValue(self, false);
                }
        }

        private IEnumerator QuitToMenu_Start(On.QuitToMenu.orig_Start orig, QuitToMenu self)
        {
                yield return null;
                UIManager ui = UIManager.instance;
                if (ui != null)
                {
                        UIManager.instance.AudioGoToGameplay(0f);
                        UnityEngine.Object.Destroy(ui.gameObject);
                }
                HeroController heroController = HeroController.instance;
                if (heroController != null)
                {
                        UnityEngine.Object.Destroy(heroController.gameObject);
                }
                GameCameras gameCameras = GameCameras.instance;
                if (gameCameras != null)
                {
                        UnityEngine.Object.Destroy(gameCameras.gameObject);
                }
                GameManager gameManager = GameManager.instance;
                if (gameManager != null)
                {
                        try
                        {
                                ObjectPool.RecycleAll();
                        }
                        catch (Exception ex)
                        {
                                Exception exception = ex;
                                Debug.LogErrorFormat("Error while recycling all as part of quit, attempting to continue regardless.");
                                Debug.LogException(exception);
                        }
                        gameManager.playerData.Reset();
                        gameManager.sceneData.Reset();
                        UnityEngine.Object.Destroy(gameManager.gameObject);
                }

                Time.timeScale = base.GlobalSettings.SpeedMultiplier;

                yield return null;

                GCManager.Collect();

                UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Menu_Title", LoadSceneMode.Single);
        }

        private IEnumerator SetTimeScale(float newTimeScale, float duration)
        {
                float lastTimeScale = Time.timeScale;
                for (float timer = 0f; timer < duration; timer += Time.unscaledDeltaTime)
                {
                        float val = Mathf.Clamp01(timer / duration);
                        SetTimeScale(Mathf.Lerp(lastTimeScale, newTimeScale, val));
                        yield return null;
                }
                SetTimeScale(newTimeScale);
        }

        private void SetTimeScale(float newTimeScale)
        {
                Time.timeScale = ((newTimeScale <= 0.01f) ? 0f : newTimeScale) * base.GlobalSettings.SpeedMultiplier;
        }

        private void GameManager_SetTimeScale_1(On.GameManager.orig_SetTimeScale_float orig, GameManager self, float newTimeScale)
        {
                Time.timeScale = ((newTimeScale <= 0.01f) ? 0f : newTimeScale) * base.GlobalSettings.SpeedMultiplier;
        }
}

