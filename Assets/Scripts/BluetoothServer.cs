using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BluetoothServer : MonoBehaviour
{
    AndroidJavaObject bluetoothServer = null;
    Pose[] pose = new Pose[2];

    delegate void HandleEventCallback(int id, int ev, byte[] data);

    private const int LEFT_INDEX = 0;
    private const int RIGHT_INDEX = 1;

    public GameObject controllerPrefab;
    private Controller[] controllers = new Controller[2];

    public Transform leftOrigin;
    public Transform rightOrigin;
    private Transform[] origin = new Transform[2];

    class HandleEventCallbackProxy : AndroidJavaProxy
    {
        HandleEventCallback callback;

        public HandleEventCallbackProxy(HandleEventCallback callback) : base("com.lanian.bluetoothserver.BluetoothRfcommServerCallback")
        {
            this.callback = callback;
        }

        public void handleEvent(int id, int ev, byte[] data)
        {
            if (callback != null)
                callback(id, ev, data);
        }


    }

    // Start is called before the first frame update
    void Start()
    {
        origin[LEFT_INDEX] = leftOrigin;
        origin[RIGHT_INDEX] = rightOrigin;

        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        Debug.Log("[LANIAN] BluetootRfcommServer Start");
        AndroidJavaClass playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = playerClass.GetStatic<AndroidJavaObject>("currentActivity");
        bluetoothServer = new AndroidJavaObject("com.lanian.bluetoothserver.BluetoothRfcommServer", new object[1] { activity });
        if (bluetoothServer != null)
        {
            bluetoothServer.Call("listenWithServiceRecord", new object[3] { "BluetoothControllerServer", "af61c678-f185-424e-acfe-7d4da5497e87", new HandleEventCallbackProxy(handleEventCallback) });
        }
        else
        {
            Debug.Log("[LANIAN] loading plugin failed");
        }

        for (int i = 0; i < pose.Length; ++i)
        {
            pose[i].position = Vector3.zero;
            pose[i].rotation = Quaternion.identity;
        }
    }

    private void OnDestroy()
    {
        if (bluetoothServer != null)
        {
            bluetoothServer.Call("cancel");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        for (int i = 0; i < controllers.Length; ++i)
        {
            if (controllers[i] != null)
            {
                controllers[i].gameObject.transform.SetPositionAndRotation(origin[i].position+pose[i].position, pose[i].rotation);
            }
        }
    }

    enum EVENT_TYPE : int
    {
        CONNECTED,
        DISCONNECTED,
        DATA_RECEIVED
    }

    enum DATA_TYPE : int
    {
        IDENTIFY = 1,
        SELECT_CLICK,
        MENU_CLICK,
        POSE
    }

    void DebugLogByteArray(string msg, byte[] data)
    {
        string s = "";
        for (int i = 0; i < data.Length; ++i)
        {
            s += string.Format("{0:X2} ", data[i]);
        }
        Debug.Log(string.Format("[LANIAN] {0} {1}", msg, s));
    }

    void handleEventCallback(int id, int ev, byte[] data)
    {
        if (ev == (int)EVENT_TYPE.DATA_RECEIVED)
        {
            if (data[0] == (int)DATA_TYPE.IDENTIFY)
            {
                DebugLogByteArray("data", data);
                byte[] msg = new byte[data.Length-1];
                for (int i = 1; i < data.Length; ++i) msg[i - 1] = data[i];
                string identity = System.Text.Encoding.UTF8.GetString(msg);
                if (identity.Equals("LEFT"))
                {
                    Debug.Log("[LANIAN] LEFT Controller");
                    ControllerConnected(LEFT_INDEX, id);
                }
                else if (identity.Equals("RIGHT"))
                {
                    Debug.Log("[LANIAN] RIGHT Controller");
                    ControllerConnected(RIGHT_INDEX, id);
                }
            }
            else if (data[0] == (int)DATA_TYPE.SELECT_CLICK)
            {
                Grip(IdToIndex(id), data[1] == 1);
            }
            else if (data[0] == (int)DATA_TYPE.MENU_CLICK)
            {
                Debug.Log("[LANIAN] MENU_CLICK");
                ToggleMenu(IdToIndex(id));
            }
            else if (data[0] == (int)DATA_TYPE.POSE)
            {
                int index = IdToIndex(id);
                if (index == LEFT_INDEX || index == RIGHT_INDEX)
                {
                    pose[index].position.Set(BitConverter.ToSingle(data, 1), BitConverter.ToSingle(data, 5), -BitConverter.ToSingle(data, 9));
                    Quaternion q = new Quaternion(BitConverter.ToSingle(data, 13), BitConverter.ToSingle(data, 17), BitConverter.ToSingle(data, 21), BitConverter.ToSingle(data, 25));
                    pose[index].rotation = Quaternion.Euler(new Vector3(-q.eulerAngles.x, -q.eulerAngles.y, q.eulerAngles.z + 90));
                }
            }
        }
    }

    void ControllerConnected(int index, int id)
    {
        if (index < 0 || index > 1)
            return;

        if (controllers[index] == null)
        {
            controllers[index] = Instantiate(controllerPrefab, origin[index].position, Quaternion.identity).GetComponent<Controller>();
        }
        controllers[index].id = id;
        if (index == LEFT_INDEX)
            controllers[index].SetLeftMode();
        else
            controllers[index].SetRightMode();
    }



    void Grip(int index, bool b)
    {
        if (index < 0 || index > 1)
            return;

        if (controllers[index] != null)
            controllers[index].SetPanel(b);
    }

    private bool[] menuState = { false, false };
    void ToggleMenu(int index)
    {
        if (index < 0 || index > 1)
            return;

        if (controllers[index] != null)
        {
            menuState[index] = !menuState[index];
            controllers[index].SetMenu(menuState[index]);
        }
        
    }

    int IdToIndex(int id)
    {
        for (int i = 0; i < controllers.Length; ++i)
        {
            if (controllers[i].id == id)
                return i;
        }
        return -1;
    }
}
