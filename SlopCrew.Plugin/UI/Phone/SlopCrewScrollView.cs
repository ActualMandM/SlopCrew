using Reptile;
using Reptile.Phone;
using SlopCrew.Common.Proto;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Rewired.Integration.UnityUI.RewiredPointerInputModule;

namespace SlopCrew.Plugin.UI.Phone;

internal class SlopCrewScrollView : ExtendedPhoneScroll {
    private AppSlopCrew? app;

    private const float ButtonScale = 2.5f;
    private const float IconScale = 2.0f;

    private const float ButtonSpacing = 24.0f;
    private const float ButtonTopMargin = 450.0f;

    public override void Initialize(App associatedApp, RectTransform root) {
        this.app = associatedApp as AppSlopCrew;

        this.SCROLL_RANGE = AppSlopCrew.CategoryCount;
        this.SCROLL_AMOUNT = 1;
        this.OVERFLOW_BUTTON_AMOUNT = 1;
        this.SCROLL_DURATION = 0.25f;
        this.LIST_LOOPS = false;

        this.m_ButtonContainer = this.gameObject.GetComponent<RectTransform>();

        this.CreatePrefabs(AppSlopCrew.SpriteSheet);

        InitalizeScrollView();
        SetListContent(AppSlopCrew.CategoryCount);
    }

    private void CreatePrefabs(AppSpriteSheet spriteSheet) {
        var musicApp = app!.MyPhone.GetAppInstance<AppMusicPlayer>();
        var homeApp = app!.MyPhone.GetAppInstance<AppHomeScreen>();

        var musicButtonPrefab = musicApp.m_TrackList.m_AppButtonPrefab;
        var confirmArrow = homeApp.m_ScrollView.Selector.Arrow;
        var titleLabel = musicButtonPrefab.transform.Find("TitleLabel").GetComponent<TextMeshProUGUI>();

        var scaledButtonSize = AppSpriteSheet.CategoryButtonSize * ButtonScale;
        var scaledIconSize = AppSpriteSheet.CategoryIconSize * IconScale;

        // Main button
        GameObject button = new GameObject("Category Button");
        var rectTransform = button.AddComponent<RectTransform>();
        rectTransform.SetAnchorAndPivot(1.0f, 0.5f);
        rectTransform.sizeDelta = scaledButtonSize;

        // Button background
        var buttonBackgroundObject = new GameObject("Button Background");
        buttonBackgroundObject.transform.SetParent(rectTransform, false);
        var buttonBackground = buttonBackgroundObject.AddComponent<Image>();
        buttonBackground.rectTransform.sizeDelta = scaledButtonSize;

        // Icon
        var buttonIconObject = new GameObject("Button Icon");
        buttonIconObject.transform.SetParent(rectTransform, false);
        var buttonIcon = buttonIconObject.AddComponent<Image>();
        buttonIcon.rectTransform.sizeDelta = scaledIconSize;
        buttonIcon.rectTransform.anchoredPosition = new Vector2((-scaledButtonSize.x * 0.5f) + (scaledIconSize.x * 0.5f) + 32.0f, 0.0f);

        // Title
        var buttonTitle = Instantiate(titleLabel);
        buttonTitle.transform.SetParent(rectTransform, false);
        float textSize = scaledIconSize.x + 8.0f;
        buttonTitle.rectTransform.sizeDelta = new Vector2(scaledButtonSize.x - textSize, scaledButtonSize.y);
        buttonTitle.rectTransform.anchoredPosition = new Vector2(textSize, 0.0f);
        buttonTitle.SetText("Category");

        // Arrow to indicate pressing right = confirm
        var arrow = Instantiate(confirmArrow).rectTransform;
        arrow!.SetParent(rectTransform, false);
        arrow!.SetAnchorAndPivot(1.0f, 0.5f);
        arrow!.anchoredPosition = new Vector2(-arrow.sizeDelta.x - 8.0f, 0.0f);

        var component = button.AddComponent<SlopCrewButton>();
        component.InitializeButton(buttonBackground,
                                   buttonIcon,
                                   buttonTitle,
                                   arrow.gameObject,
                                   spriteSheet.CategoryButtonNormal,
                                   spriteSheet.CategoryButtonSelected);

        m_AppButtonPrefab = button;
        m_AppButtonPrefab.SetActive(false);
    }

    public override void OnButtonCreated(PhoneScrollButton newButton) {
        newButton.gameObject.SetActive(true);

        base.OnButtonCreated(newButton);
    }

    public override void SetButtonContent(PhoneScrollButton button, int contentIndex) {
        var slopCrewButton = (SlopCrewButton) button;
        var categoryType = (AppSlopCrew.Category) contentIndex;
        slopCrewButton.SetButtonContents(categoryType, AppSlopCrew.SpriteSheet.GetCategoryIcon(categoryType)!);
    }

    public override void SetButtonPosition(PhoneScrollButton button, float posIndex) {
        var buttonSize = this.m_AppButtonPrefab.RectTransform().sizeDelta.y + ButtonSpacing;
        var rectTransform = button.RectTransform();

        var newPosition = new Vector2 {
            x = rectTransform.anchoredPosition.x,
            y = ButtonTopMargin - ((posIndex - (this.SCROLL_RANGE / 2.0f)) * buttonSize) -
                (this.SCROLL_RANGE % 2.0f == 0.0f ? buttonSize / 2.0f : 0.0f)
        };

        rectTransform.anchoredPosition = newPosition;
    }
}
