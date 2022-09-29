using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameOptionsUtility
{
    [RequireComponent(typeof(Dropdown))]
    public class DropDownWindowMode : MonoBehaviour
    {
        public Dropdown refreshRateDropdown;

        private void OnEnable()
        {
            var dropdown = GetComponent<Dropdown>();
            InitializeEntries(dropdown);
            dropdown.onValueChanged.AddListener(UpdateOptions);
            UpdateOptions(dropdown.value);
        }

        private void OnDisable()
        {
            GetComponent<Dropdown>().onValueChanged.RemoveListener(UpdateOptions);
        }

        public void InitializeEntries(Dropdown dropdown)
        {
            dropdown.options.Clear();
            //dropdown.options.Add(new Dropdown.OptionData("Full Screen (Exclusive)"));
            dropdown.options.Add(new Dropdown.OptionData("Full Screen (Windowed)"));
            dropdown.options.Add(new Dropdown.OptionData("Maximized Window"));
            dropdown.options.Add(new Dropdown.OptionData("Window"));
            dropdown.value = (int)(GameOption.Get<GraphicOption>().fullScreenMode) - 1;
        }

        void UpdateOptions(int value)
        {
            //if (value == 0)
            //{
            //    value = 1;
            //    var dropdown = GetComponent<Dropdown>();
            //    dropdown.SetValueWithoutNotify(value);
            //}

            GameOption.Get<GraphicOption>().fullScreenMode = (FullScreenMode)(value + 1);
            if (refreshRateDropdown != null)
            {
                refreshRateDropdown.interactable = false;
                refreshRateDropdown.captionText.CrossFadeAlpha(1.0f, refreshRateDropdown.colors.fadeDuration, true);
            }
        }
    }

}