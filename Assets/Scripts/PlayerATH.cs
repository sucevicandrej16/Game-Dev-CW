using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro; 

public class PlayerATH : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider staminaSlider;

    [SerializeField] private TextMeshProUGUI _healthSliderText;
    [SerializeField] private TextMeshProUGUI _staminaSliderText;

    public void UpdateHealthBar(float currHealth, float maxHealth){
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currHealth;
    }

    public void UpdateStaminaBar(float currStamina, float maxStamina){
        staminaSlider.maxValue= maxStamina;
        staminaSlider.value = currStamina;
        Debug.Log("stamina slider value" + currStamina);
    }

    void Start(){
        healthSlider.onValueChanged.AddListener((v) => {
            _healthSliderText.text = v.ToString("0");
        });

        staminaSlider.onValueChanged.AddListener((v) => {
            Debug.Log("V" + v);
            _staminaSliderText.text = v.ToString("0");
        });
    }
}