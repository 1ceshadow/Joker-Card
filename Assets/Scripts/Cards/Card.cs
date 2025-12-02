using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// 卡牌UI组件（交互 + 显示）
/// 位置：Assets/Scripts/Cards/Card.cs
/// 功能：负责卡牌的UI交互（拖拽、选择、视觉效果），数据由CardData承载
/// </summary>
public class Card : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, 
    IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    /// <summary>
    /// 卡牌数据（花色、点数等）
    /// </summary>
    public CardData cardData;

    private Canvas canvas;
    private Image imageComponent;

    // 视觉系统
    [Header("Visual")]
    [SerializeField] private bool instantiateVisual = true;
    [SerializeField] private GameObject cardVisualPrefab;
    [HideInInspector] public CardVisual cardVisual;
    private VisualCardsHandler visualHandler;

    // 移动与拖拽
    [Header("Movement")]
    [SerializeField] private float moveSpeedLimit = 50;
    private Vector3 offset;

    // 选择系统
    [Header("Selection")]
    public bool selected;
    public float selectionOffset = 50;
    private float pointerDownTime;
    private float pointerUpTime;

    // 状态标记
    [Header("States")]
    public bool isHovering;
    public bool isDragging;
    [HideInInspector] public bool wasDragged;

    // 事件系统
    [Header("Events")]
    [HideInInspector] public UnityEvent<Card> PointerEnterEvent;
    [HideInInspector] public UnityEvent<Card> PointerExitEvent;
    [HideInInspector] public UnityEvent<Card, bool> PointerUpEvent;
    [HideInInspector] public UnityEvent<Card> PointerDownEvent;
    [HideInInspector] public UnityEvent<Card> BeginDragEvent;
    [HideInInspector] public UnityEvent<Card> EndDragEvent;
    [HideInInspector] public UnityEvent<Card, bool> SelectEvent;


    // Unity 生命周期
    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        imageComponent = GetComponent<Image>();

        if (!instantiateVisual)
            return;

        visualHandler = FindFirstObjectByType<VisualCardsHandler>();
        if (cardVisualPrefab != null)
        {
            GameObject visualObj = Instantiate(cardVisualPrefab, visualHandler ? visualHandler.transform : canvas.transform);
            cardVisual = visualObj.GetComponent<CardVisual>();
            if (cardVisual != null)
            {
                cardVisual.Initialize(this);
            }
        }
    }

    void Update()
    {
        ClampPosition();

        if (isDragging)
        {
            Vector2 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) - offset;
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            Vector2 velocity = direction * Mathf.Min(moveSpeedLimit, Vector2.Distance(transform.position, targetPosition) / Time.deltaTime);
            transform.Translate(velocity * Time.deltaTime);
        }
    }

    private void OnDestroy()
    {
        if (cardVisual != null)
            Destroy(cardVisual.gameObject);
    }

    // 初始化方法
    /// <summary>
    /// 初始化卡牌（设置数据）
    /// </summary>
    public void Initialize(CardData data)
    {
        cardData = data;
        UpdateVisual();
    }

    /// <summary>
    /// 更新卡牌视觉（根据cardData）
    /// </summary>
    public void UpdateVisual()
    {
        if (cardData == null)
            return;

        // TODO: 更新卡牌图片/文本显示
        // 这里需要根据 cardData.suit 和 cardData.rank 来设置对应的sprite
        // 示例：imageComponent.sprite = CardSpriteLoader.GetCardSprite(cardData);
    }

    // 位置控制
    void ClampPosition()
    {
        if (Camera.main == null)
            return;

        Vector2 screenBounds = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -screenBounds.x, screenBounds.x);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, -screenBounds.y, screenBounds.y);
        transform.position = new Vector3(clampedPosition.x, clampedPosition.y, 0);
    }
    // 位置控制

    // 拖拽事件处理
    public void OnBeginDrag(PointerEventData eventData)
    {
        BeginDragEvent.Invoke(this);
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = mousePosition - (Vector2)transform.position;
        isDragging = true;
        
        if (canvas != null)
        {
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = false;
        }
        
        if (imageComponent != null)
            imageComponent.raycastTarget = false;

        wasDragged = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 拖拽逻辑在Update中处理
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndDragEvent.Invoke(this);
        isDragging = false;
        
        if (canvas != null)
        {
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = true;
        }
        
        if (imageComponent != null)
            imageComponent.raycastTarget = true;

        StartCoroutine(FrameWait());

        IEnumerator FrameWait()
        {
            yield return new WaitForEndOfFrame();
            wasDragged = false;
        }
    }

    // 指针事件处理
    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEnterEvent.Invoke(this);
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExitEvent.Invoke(this);
        isHovering = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        PointerDownEvent.Invoke(this);
        pointerDownTime = Time.time;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        pointerUpTime = Time.time;

        PointerUpEvent.Invoke(this, pointerUpTime - pointerDownTime > .2f);

        if (pointerUpTime - pointerDownTime > .2f)
            return;

        if (wasDragged)
            return;

        selected = !selected;
        SelectEvent.Invoke(this, selected);

        if (cardVisual != null)
        {
            if (selected)
                transform.localPosition += (cardVisual.transform.up * selectionOffset);
            else
                transform.localPosition = Vector3.zero;
        }
        else
        {
            if (selected)
                transform.localPosition += Vector3.up * selectionOffset;
            else
                transform.localPosition = Vector3.zero;
        }
    }
    // 选择控制
    /// <summary>
    /// 取消选择状态
    /// </summary>
    public void Deselect()
    {
        if (selected)
        {
            selected = false;
            transform.localPosition = Vector3.zero;
        }
    }

    // 层级与位置查询
    public int SiblingAmount()
    {
        return transform.parent != null && transform.parent.CompareTag("Slot") 
            ? transform.parent.parent.childCount - 1 
            : 0;
    }

    public int ParentIndex()
    {
        return transform.parent != null && transform.parent.CompareTag("Slot") 
            ? transform.parent.GetSiblingIndex() 
            : 0;
    }

    public float NormalizedPosition()
    {
        if (transform.parent == null || !transform.parent.CompareTag("Slot"))
            return 0;
            
        int parentCount = transform.parent.parent.childCount - 1;
        if (parentCount <= 0)
            return 0;
            
        return ExtensionMethods.Remap((float)ParentIndex(), 0, (float)parentCount, 0, 1);
    }
}

