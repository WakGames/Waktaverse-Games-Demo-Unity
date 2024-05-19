using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AchieveTest : MonoBehaviour
{
    public Animator anim;
    // Start is called before the first frame update
    void Start()
    {
        anim.SetBool("BtnPush", false);
    }

    // Update is called once per frame
    void Update()
    {
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("ResetBtnPush"))
        {
            Debug.Log(anim.GetBool("BtnPush"));
            anim.SetBool("BtnPush", false);
        }
    }

    public void OnBtnTestClicked()
    {   
        anim.SetBool("BtnPush", true);
        Debug.Log("Test Button Clicked");
    }
}
