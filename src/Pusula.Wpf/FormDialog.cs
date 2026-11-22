using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;
using DesignKit.Controls;

namespace Pusula.Wpf
{
    /// <summary>
    /// Doğrulamalı form penceresi. DesignKit <see cref="ContentDialog"/> birincil düğmede kendiliğinden kapanır;
    /// burada düğme olayında <see cref="KeepOpen"/> çağrılırsa pencere açık kalır (kapanıp yeniden açılmaz,
    /// kırpışma olmaz).
    /// </summary>
    public class FormDialog : ContentDialog
    {
        private static readonly MethodInfo? BasePrimaryClick =
            typeof(ContentDialog).GetMethod("OnPrimaryClick", BindingFlags.NonPublic | BindingFlags.Instance);

        private ButtonBase? primaryButton;
        private bool keepOpen;

        /// <summary>Birincil düğme olayından çağrılır: bu tıklamada pencere kapanmaz.</summary>
        public void KeepOpen()
        {
            keepOpen = true;
        }

        /// <summary>Şablonu uygular; birincil düğmenin kendiliğinden kapatan işleyicisini kendi işleyicisiyle değiştirir.</summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (primaryButton != null)
            {
                primaryButton.Click -= OnFormPrimaryClick;
            }

            primaryButton = GetTemplateChild("PART_PrimaryButton") as ButtonBase;
            if (primaryButton != null && BasePrimaryClick != null)
            {
                var baseHandler = (RoutedEventHandler)System.Delegate.CreateDelegate(typeof(RoutedEventHandler), this, BasePrimaryClick);
                primaryButton.Click -= baseHandler;
                primaryButton.Click += OnFormPrimaryClick;
            }
        }

        private void OnFormPrimaryClick(object sender, RoutedEventArgs e)
        {
            keepOpen = false;
            RaiseEvent(new RoutedEventArgs(PrimaryButtonClickEvent, this));
            if (keepOpen)
            {
                keepOpen = false;
                return;
            }

            IsOpen = false;
        }
    }
}
