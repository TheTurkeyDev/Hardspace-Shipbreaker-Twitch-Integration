using BBI.Unity.Game;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO.Pipes;
using System.IO;
using System.Collections.Concurrent;
using Harmony;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine;
using System.Reflection;
using static BBI.Unity.Game.GameSession;
using BBI;

namespace Shipbreaker_Integration
{
    class TIMain
    {
        public static void InitMod()
        {
            StartConnection();
            FileLog.Log("Init Mod");
            SceneManager.activeSceneChanged += new UnityAction<Scene, Scene>(NotificationUI.AttachWindow);
            HarmonyInstance.Create("com.theprogrammingturkey.HSSTwitchIntegration").PatchAll(Assembly.GetExecutingAssembly());
        }


        public static ConcurrentQueue<RewardData> rewardsQueue = new ConcurrentQueue<RewardData>();

        [HarmonyPatch(typeof(Main), "LateUpdate")]
        class Patch
        {
            static void Prefix()
            {
                GameSession gs = Main.Instance.CurrentGameSession;
                if (gs != null && CurrentGameState == GameState.Gameplay)
                {
                    Rigidbody body = LynxPlayerController.Instance.PlayerRigidbody;
                    RewardData reward;
                    if (rewardsQueue.TryDequeue(out reward))
                    {
                        switch (reward.action)
                        {
                            case "push":
                                float forceMin;
                                float.TryParse(reward.args[0], out forceMin);
                                float forceMax;
                                float.TryParse(reward.args[1], out forceMax);
                               
                                body.AddForce(UnityEngine.Random.onUnitSphere * (UnityEngine.Random.Range(forceMin, forceMax)));
                                break;
                            case "test":
                                Player player = LynxPlayerController.Instance.Player;
                                body.AddRelativeTorque(player.transform.forward * 25);
                                break;
                        }
                    }
                }
            }
        }

        //public void OnBindingAdded(PlayerAction action, BindingSource source){ }

        private static readonly CancellationTokenSource source = new CancellationTokenSource();

        //Tells the connection to shut down
        public static void Shutdown()
        {
            source.Cancel();
        }

        //Starts and handles the connection
        public static void StartConnection()
        {
            CancellationToken token = source.Token;
            if (!token.IsCancellationRequested)
            {
                //Starts the connection task on a new thread
                Task.Factory.StartNew(() =>
                {
                    //Keep making new pipes
                    while (!token.IsCancellationRequested)
                    {
                        //Catch any errors
                        try
                        {
                            //pipeName is the same as your subfolder name in the Integrations folder of the app
                            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "Shipbreaker", PipeDirection.In))
                            {
                                using (StreamReader reader = new StreamReader(client))
                                {
                                    //Keep trying to connect
                                    while (!token.IsCancellationRequested && !client.IsConnected)
                                    {
                                        try
                                        {
                                            client.Connect(1000);//Don't wait too long, so mod can shut down quickly if still trying to connect
                                        }
                                        catch (TimeoutException)
                                        {
                                            //Ignore
                                        }
                                        catch (System.ComponentModel.Win32Exception)
                                        {
                                            //Ignore and sleep for a bit, since the connection didn't time out
                                            Thread.Sleep(500);
                                        }
                                    }
                                    //Keep trying to read
                                    while (!token.IsCancellationRequested && client.IsConnected && !reader.EndOfStream)
                                    {

                                        //Read line from stream
                                        string line = reader.ReadLine();

                                        if (line != null)
                                        {
                                            FileLog.Log("Packet: " + line);
                                            if (line.StartsWith("Action: "))
                                            {
                                                string[] data = line.Substring(8).Split(' ');
                                                rewardsQueue.Enqueue(new RewardData(data[0], data.Skip(1).ToArray()));
                                                //Handle action message. This is what was generated by your IntegrationAction Execute method
                                                //Make sure you handle it on the correct thread
                                            }
                                            else if (line.StartsWith("Message: "))
                                            {
                                                string message = line.Substring(9);
                                                NotificationUI.addChatMessage(message);
                                            }
                                        }
                                        //Only read every 50ms
                                        Thread.Sleep(50);
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            //Ignore
                        }
                    }

                }, token);
            }
        }

        public class RewardData
        {
            public string action;
            public string[] args;

            public RewardData(string action, string[] args)
            {
                this.action = action;
                if (args == null)
                    this.args = new string[0];
                else
                    this.args = args;
            }
        }
    }
}
