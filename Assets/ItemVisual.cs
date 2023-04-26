using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


public class ItemVisual : VisualElement
{

  public new class UxmlFactory : UxmlFactory<ItemVisual, UxmlTraits> { }

  public new class UxmlTraits : VisualElement.UxmlTraits { }

  public static readonly string USSClassName = "draggable-visual";

  private int _originalIndex;
  private bool _mIsDragging;

  private ScrollView _parentScroll;
  private bool _canPlace;
  private int _currentIndex;

  public ItemVisual()
  {
    AddToClassList(USSClassName);

    //Register the mouse callbacks
    //RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
    //RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
    //RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
    //RegisterCallback<MouseLeaveEvent>(OnMouseLeaveEvent);
  }


  ~ItemVisual()
  {
    //UnregisterCallback<MouseLeaveEvent>(OnMouseLeaveEvent);
    //UnregisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
    //UnregisterCallback<MouseDownEvent>(OnMouseDownEvent);
    //UnregisterCallback<MouseUpEvent>(OnMouseUpEvent);
  }


  /// <summary>
  /// Gets the mouse position and converts it to the right position relative to the grid part of the UI
  /// </summary>
  /// <param name="mousePosition">Current mouse position</param>
  /// <returns>Converted mouse position relative to the Grid part of the UI</returns>
  public Vector2 GetMousePosition(Vector2 mousePosition) => new(
    mousePosition.x - (layout.width / 2) - parent.worldBound.position.x,
    mousePosition.y - (layout.height / 2) - parent.worldBound.position.y);

  /// <summary>
  /// Set the position of the element
  /// </summary>
  /// <param name="pos">New position</param>
  public void SetPosition(int index)
  {
    //if(parentScroll.Contains(this))
    //  parentScroll.Remove(this);
    //style.position = new StyleEnum<Position>(Position.Relative);
    //style.top = 0;
    //style.left = 0;
    //parentScroll.Insert(originalIndex, this);
  }

  /// <summary>
  /// Handles logic for when the mouse has been released
  /// </summary>
  private void OnMouseDownEvent(MouseDownEvent mouseEvent)
  {
    if (!_mIsDragging)
    {
      StartDrag(mouseEvent);
     // PlayerInventory.UpdateItemDetails(m_Item);
      return;
    }

    
  }


  private void OnMouseUpEvent(MouseUpEvent evt)
  {
    if (!_mIsDragging)
      return;

    _mIsDragging = false;

    if (_canPlace)
      SetPosition(_currentIndex);
    else
      SetPosition(_originalIndex);
  }


  private void OnMouseLeaveEvent(MouseLeaveEvent evt)
  {
    if (!_mIsDragging)
      return;

    Debug.Log("stop dragging . left mouse");
    SetPosition(_originalIndex);
  }

  /// <summary>
  /// Starts the dragging logic
  /// </summary>
  public void StartDrag(MouseDownEvent mouseEvent)
  {
    if(_parentScroll == null)
      _parentScroll = parent as ScrollView;
    
    Debug.Log("start dragging");
    _mIsDragging = true;
    _originalIndex = _parentScroll.IndexOf(this);
    _parentScroll.Remove(this);
    style.position = new StyleEnum<Position>(Position.Absolute);
    
    style.left = 10;
    style.top = mouseEvent.mousePosition.y - _parentScroll.worldBound.y + (mouseEvent.mousePosition.y - worldBound.y);
    BringToFront();
  }


  /// <summary>
  /// Handles logic for every time the mouse moves. Only runs if the player is actively dragging
  /// </summary>
  private void OnMouseMoveEvent(MouseMoveEvent mouseEvent)
  {
    if (!_mIsDragging)
    {
      return;
    }
    //int index = parentScroll.IndexOf(this);
    //if (index != -1)
    //{
    //  parentScroll.Remove(this);
    //  originalIndex = index;
    //}
    Debug.Log("dragging");
    style.left = 10;
    style.top = mouseEvent.mousePosition.y - _parentScroll.worldBound.y + worldBound.y + (mouseEvent.mousePosition.y - worldBound.y);
    style.position = new StyleEnum<Position>(Position.Absolute);
    _currentIndex = 1;
    foreach(var item in _parentScroll.Children())
    {
      if (item.worldBound.Contains(mouseEvent.mousePosition))
      {
        _canPlace = true;
        _parentScroll.Remove(this);
        SetPosition(_currentIndex);
        break; 
      }
      _currentIndex++;
    }

    SetPosition(_currentIndex);
  }

}