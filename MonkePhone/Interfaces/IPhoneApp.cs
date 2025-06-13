using MonkePhone.Behaviours.UI;

namespace MonkePhone.Interfaces
{
    public interface IPhoneApp
    {
        string AppId { get; }
        void AppOpened();
        void AppClosed();
        void ButtonClick(PhoneUIObject phoneUIObject, bool isLeftHand);
        void ButtonTicked(PhoneUIObject phoneUIObject, bool currentValue, bool isLeftHand);
        void SliderUpdated(PhoneUIObject phoneUIObject, float currentValue, bool isSelected);
    }
}
