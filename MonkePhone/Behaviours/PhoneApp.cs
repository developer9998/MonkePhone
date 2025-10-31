using System.Linq;
using MonkePhone.Behaviours.UI;
using MonkePhone.Interfaces;

namespace MonkePhone.Behaviours;

public abstract class PhoneApp : PhoneBehaviour, IPhoneApp
{
    public bool Opened => PhoneManager.Instance.AppOpened(this);

    public virtual void Update(bool isBackgroundApp) { }

    public abstract string AppId { get; }

    /// <summary>
    ///     The AppOpened method is invoked when the app has been opened by the phone.
    /// </summary>
    public virtual void AppOpened() { }

    /// <summary>
    ///     The AppClosed method is invoked when the app has been closed by the phone.
    /// </summary>
    public virtual void AppClosed() { }

    /// <summary>
    ///     The ButtonClick method is invoked when a button contained in the app has been clicked. (i.e. a PhoneButton)
    /// </summary>
    public virtual void ButtonClick(PhoneUIObject phoneUIObject, bool isLeftHand) { }

    /// <summary>
    ///     The ButtonTicked method is invoked when a button contained in the app has been ticked. (i.e. a PhoneCheckbox or
    ///     PhoneSwitch)
    /// </summary>
    public virtual void ButtonTicked(PhoneUIObject phoneUIObject, bool currentValue, bool isLeftHand) { }

    /// <summary>
    ///     The SliderUpdated method is invoked when a slider contained in the app has been updated. (i.e. a PhoneSlider)
    /// </summary>
    public virtual void SliderUpdated(PhoneUIObject phoneUIObject, float currentValue, bool isSelected) { }

    public virtual void Initialize() { }

    public T GetApp<T>() where T : PhoneApp => PhoneManager.Instance.GetApp<T>();

    public PhoneUIObject GetObject(string objectName, bool includeInactive = false) => gameObject
           .GetComponentsInChildren<PhoneUIObject>(includeInactive)
           .FirstOrDefault(phoneUiObject => phoneUiObject.name == objectName);
}