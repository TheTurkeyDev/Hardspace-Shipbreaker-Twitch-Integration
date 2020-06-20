using BBI.Unity.Game;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Shipbreaker_Integration
{
    class NotificationUI : MonoBehaviour
    {
        private static GUIStyle guiStyle = new GUIStyle();

        private static List<string> queue = new List<string>();

        private static bool display = false;
        private static int displayLength = 5000;
        private static DateTime displayStart;

        public static void AttachWindow(Scene oldScene, Scene newScene)
        {
            guiStyle.fontSize = 20;
            guiStyle.wordWrap = true;
            guiStyle.padding = new RectOffset(15, 15, 15, 15);
            DontDestroyOnLoad(new GameObject("_ChatWindow", new Type[1]
            {
                typeof (NotificationUI)
            }));
            SceneManager.activeSceneChanged -= new UnityAction<Scene, Scene>(AttachWindow);
        }

        public static void addChatMessage(string message)
        {
            queue.Add(message);
            if (queue.Count == 1)
            {
                display = true;
                displayStart = DateTime.UtcNow;
            }
        }

        private void OnGUI()
        {
            if (Main.Instance.CurrentGameSession != null && display)
            {
                float timeIn = (float)(DateTime.UtcNow - displayStart).TotalMilliseconds;
                float width = 0;
                if (timeIn < 500)
                {
                    float completePercent = timeIn / 500;
                    width = 400 * completePercent;
                    guiStyle.normal.textColor = new Color(Color.white.r, Color.white.g, Color.white.b, completePercent);
                }
                else if (timeIn >= displayLength - 500)
                {
                    float completePercent = (displayLength - timeIn) / 500;
                    width = 400 * completePercent;
                    guiStyle.normal.textColor = new Color(Color.white.r, Color.white.g, Color.white.b, completePercent);
                }
                else
                {
                    width = 400f;
                    guiStyle.normal.textColor = Color.white;
                }

                GUI.Box(new Rect((Screen.width / 2) - 200, 200, width, 100f), "");
                GUI.Label(new Rect((Screen.width / 2) - 200, 200, 400f, 100f), queue[0], guiStyle);

                if (timeIn / displayLength > 1)
                {
                    queue.RemoveAt(0);
                    if (queue.Count == 0)
                        display = false;
                    else
                        displayStart = DateTime.UtcNow;
                }
            }
        }
    }
}
