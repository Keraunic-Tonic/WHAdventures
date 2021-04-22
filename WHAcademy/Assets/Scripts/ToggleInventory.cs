using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleInventory : MonoBehaviour
{
    public Button inventoryButton;
    private bool isImageOn;
    public Image panelImage;

    // Start is called before the first frame update
    void Start()
    {
        Button btn = inventoryButton.GetComponent<Button>();
        btn.onClick.AddListener(ToggleInventoryPanel);

        panelImage.enabled = false;
        isImageOn = false;
    }

    void ToggleInventoryPanel()
    {
        Debug.Log("The inventory button has been clicked");
        if (isImageOn == true)
        {
            panelImage.enabled = false;
            isImageOn = false;
        }
        else
        {
            panelImage.enabled = true;
            isImageOn = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
