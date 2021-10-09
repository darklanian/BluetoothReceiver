using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public GameObject panel;
    public GameObject left;
    public GameObject right;
    public GameObject menu;

    private Color panelOffColor;
    public Color panelOnColor;

    private Color menuOffColor;
    public Color menuOnColor;

    [HideInInspector]
    public int id = -1;

    // Start is called before the first frame update
    void Start()
    {
        panelOffColor = panel.GetComponent<Renderer>().material.color;
        menuOffColor = menu.GetComponent<Renderer>().material.color;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetPanel(bool on)
    {
        panel.GetComponent<Renderer>().material.color = on ? panelOnColor : panelOffColor;
    }

    public void SetLeftMode()
    {
        left.SetActive(true);
        right.SetActive(false);
    }

    public void SetRightMode()
    {
        right.SetActive(true);
        left.SetActive(false);
    }

    public void SetMenu(bool b)
    {
        Debug.Log(string.Format("[LANIAN] SetMenu {0}", b));
        menu.GetComponent<Renderer>().material.color = b ? menuOnColor : menuOffColor;
    }
}
