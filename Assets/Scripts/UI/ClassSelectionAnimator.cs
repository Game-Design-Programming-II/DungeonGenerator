using System;
using System.Collections.Generic;
using ClassSystem.Classes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClassSelectionAnimator : MonoBehaviour
{
    [Header("Animation/Audio Settings")]
    [SerializeField] private ClassSelectionAnimator[] _buttons;
    [SerializeField] private Animator _anim;
    [SerializeField] private AudioSource _aS;
    [SerializeField, Tooltip("Reference to the class selection controller.")]
    private ClassSelectUI _classUI;
    [Tooltip("The ID of the class this button selects.")]
    [SerializeField, Range(0,2)] private uint _classID;
    private bool _selected;

    private void OnMouseEnter()
    {
        _anim.SetBool("hovered", true);
        
    }

    private void OnMouseExit()
    {
        if (!_selected)
        {
            _anim.SetBool("hovered", false);
        }
    }

    private void OnMouseDown()
    {
        _selected = !_selected;
        _classUI?.Select((int)_classID);
        if (_selected)
        {
            foreach (ClassSelectionAnimator item in _buttons)
            {
                item.IsSelected(false);
            }
        }
        _aS.Play();
    }

    public void IsSelected(bool boo)
    {
        _selected = boo;
        _anim.SetBool("hovered", false);
    }
}
