using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

public class ToggleInventory : MonoBehaviour
{
    public Button inventoryButton;
    private bool isImageOn;
    public Image panelImage;
    public Sprite closedBagSprite;
    public Sprite openBagSprite;
    public GameObject inventoryGrid;
    public Button leftButton;
    public Button rightButton;

    // Start is called before the first frame update
    void Start()
    {
        Button btn = inventoryButton.GetComponent<Button>();
        btn.onClick.AddListener(ToggleInventoryPanel);

        panelImage.enabled = false;
        isImageOn = false;
        leftButton.image.enabled = false;
        rightButton.image.enabled = false;
        inventoryGrid.SetActive(false);
    }

    void ToggleInventoryPanel()
    {
        Debug.Log("The inventory button has been clicked");
        if (isImageOn == true) //turning off the inventory
        {
            inventoryGrid.SetActive(false);
            panelImage.enabled = false;
            isImageOn = false;
            leftButton.image.enabled = false;
            rightButton.image.enabled = false;
            inventoryButton.image.sprite = closedBagSprite;
        }
        else // turning on the inventory
        {
            inventoryGrid.SetActive(true);
            panelImage.enabled = true;
            isImageOn = true;
            leftButton.image.enabled = true;
            rightButton.image.enabled = true;
            inventoryButton.image.sprite = openBagSprite;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
