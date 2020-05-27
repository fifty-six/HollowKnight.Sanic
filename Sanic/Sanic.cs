using Modding;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sanic
{
    public class Sanic : Mod
    {
        public sealed override ModSettings GlobalSettings
        {
            get => _globalSettings;
            set => _globalSettings = (GlobalModSettings) value;
        }
        
        private readonly FieldInfo timeSlowedField;
        
        private GlobalModSettings _globalSettings = new GlobalModSettings();

        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        
        public Sanic()
        {
            timeSlowedField = typeof(GameManager).GetField("timeSlowed", BindingFlags.NonPublic | BindingFlags.Instance);

            Time.timeScale = _globalSettings.SpeedMultiplier;
            ModHooks.Instance.HeroUpdateHook += Instance_HeroUpdateHook;
            On.GameManager.SetTimeScale_float += GameManager_SetTimeScale_1;
            On.GameManager.FreezeMoment_float_float_float_float += GameManager_FreezeMoment_1;
            On.QuitToMenu.Start += QuitToMenu_Start;
        }

        private void Instance_HeroUpdateHook()
        {
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                if (Math.Abs(Time.timeScale - _globalSettings.SpeedMultiplier) < Mathf.Epsilon)
                    Time.timeScale += 0.05f;

                _globalSettings.SpeedMultiplier += 0.05f;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if (Math.Abs(Time.timeScale - _globalSettings.SpeedMultiplier) < Mathf.Epsilon)
                    Time.timeScale -= 0.05f;

                _globalSettings.SpeedMultiplier -= 0.05f;
            }
        }

        private IEnumerator GameManager_FreezeMoment_1
        (
            On.GameManager.orig_FreezeMoment_float_float_float_float orig,
            GameManager self,
            float rampDownTime,
            float waitTime,
            float rampUpTime,
            float targetSpeed
        )
        {
            if ((bool) timeSlowedField.GetValue(self)) yield break;
            
            timeSlowedField.SetValue(self, true);
            
            yield return self.StartCoroutine(SetTimeScale(targetSpeed, rampDownTime));
            
            for (float timer = 0f; timer < waitTime; timer += Time.unscaledDeltaTime * _globalSettings.SpeedMultiplier)
            {
                yield return null;
            }

            yield return self.StartCoroutine(SetTimeScale(1f, rampUpTime));
            
            timeSlowedField.SetValue(self, false);
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
                catch (Exception exception)
                {
                    Debug.LogErrorFormat("Error while recycling all as part of quit, attempting to continue regardless.");
                    Debug.LogException(exception);
                }

                gameManager.playerData.Reset();
                gameManager.sceneData.Reset();
                UnityEngine.Object.Destroy(gameManager.gameObject);
            }

            Time.timeScale = _globalSettings.SpeedMultiplier;
            
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
            Time.timeScale = (newTimeScale <= 0.01f ? 0f : newTimeScale) * _globalSettings.SpeedMultiplier;
        }

        private void GameManager_SetTimeScale_1(On.GameManager.orig_SetTimeScale_float orig, GameManager self, float newTimeScale)
        {
            Time.timeScale = (newTimeScale <= 0.01f ? 0f : newTimeScale) * _globalSettings.SpeedMultiplier;
        }
    }

    public class GlobalModSettings : ModSettings
    {
        public float SpeedMultiplier = 1.3f;
    }
}
