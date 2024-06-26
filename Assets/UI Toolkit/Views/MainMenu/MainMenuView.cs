using System.Collections;

using CodeBlaze.Vloxy.Engine.Settings;
using CodeBlaze.Vloxy.Engine.Utils.Logger;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace CodeBlaze {
    public class MainMenuView : MonoBehaviour {

        [SerializeField] private VloxySettings _settings;

        private Button _generate_button;
        private Button _quit_button;
        private ProgressBar _loader;

        private MainMenuController _controller;

        private void OnEnable() {
            var document = GetComponent<UIDocument>();

            _controller = new MainMenuController(document.rootVisualElement);

            _generate_button = document.rootVisualElement.Q<Button>("Generate");
            _quit_button = document.rootVisualElement.Q<Button>("Quit");
            _loader = document.rootVisualElement.Q<ProgressBar>("LoadingBar");

            _generate_button.RegisterCallback<ClickEvent>(OnGenerateWorld);
            _quit_button.RegisterCallback<ClickEvent>(OnQuit);
        }

        private void OnQuit(ClickEvent _) {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; 
#endif
        }

        private void OnGenerateWorld(ClickEvent _) {
            _controller.SetValue(_settings);
#if VLOXY_LOGGING
            VloxyLogger.Info<MainMenuView>("Generating World");
#endif
            StartCoroutine(GenerateWorld());
        }

        private IEnumerator GenerateWorld() {
            _loader.visible = true;

            var loader = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1);

            while(!loader.isDone) {
                _loader.lowValue = loader.progress * 100;
                yield return null;
            }
        }
        
        private bool IsEscapePressedThisFrame()
        {
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        }

    }
}
