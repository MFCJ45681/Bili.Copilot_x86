// Copyright (c) Bili Copilot. All rights reserved.

using Microsoft.UI.Input;

namespace BiliCopilot.UI.Toolkits;

/// <summary>
/// 触控与触控板适配工具.
/// </summary>
public static class TouchAdaptToolkit
{
    /// <summary>
    /// 触屏友好的最小命中区域(微软触控设计指南建议 44x44 DIP，桌面端取 40 DIP).
    /// </summary>
    public const double MinTouchTargetSize = 40d;

    /// <summary>
    /// 是否为触摸优先设备(纯触屏平板，无鼠标).
    /// </summary>
    public static bool IsTouchPrimary { get; private set; }

    /// <summary>
    /// 环境切换为触控优先时触发，已加载的控件可据此补做热区放大.
    /// </summary>
    public static event Action? TouchEnvironmentEntered;

    /// <summary>
    /// 初始化触控环境监测，需在应用启动时调用一次.
    /// </summary>
    public static void Initialize()
    {
        // 初始按非平板处理；一旦收到触摸/笔输入即切换为触控场景。
        IsTouchPrimary = false;
    }

    /// <summary>
    /// 通知已收到一次触摸输入(用于把环境切换为触控优先).
    /// </summary>
    public static void NotifyTouchInput()
    {
        if (IsTouchPrimary)
        {
            return;
        }

        IsTouchPrimary = true;
        TouchEnvironmentEntered?.Invoke();
    }

    /// <summary>
    /// 判断指针是否应按触摸方式处理(触屏、手写笔，或触控板模拟的触摸).
    /// </summary>
    /// <param name="pointerType">指针设备类型.</param>
    /// <returns>是否按触摸处理.</returns>
    public static bool ShouldTreatAsTouch(PointerDeviceType pointerType)
        => pointerType is PointerDeviceType.Touch or PointerDeviceType.Pen;

    /// <summary>
    /// 将按钮的命中区域放大到触控友好尺寸(不改变图标视觉尺寸).
    /// </summary>
    /// <param name="button">目标按钮.</param>
    public static void EnsureTouchTarget(Microsoft.UI.Xaml.Controls.Button button)
    {
        if (button is null)
        {
            return;
        }

        if (button.MinWidth < MinTouchTargetSize)
        {
            button.MinWidth = MinTouchTargetSize;
        }

        if (button.MinHeight < MinTouchTargetSize)
        {
            button.MinHeight = MinTouchTargetSize;
        }
    }
}
