using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace RackMonitor.Behaviors
{
    public class PasswordBoxAssistant : Behavior<PasswordBox>
    {

        public static readonly DependencyProperty BoundPasswordProperty = DependencyProperty.Register(nameof(BoundPassword), typeof(SecureString), typeof(PasswordBoxAssistant), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundPasswordChanged));

        public SecureString BoundPassword
        {
            get { return (SecureString)GetValue(BoundPasswordProperty); }
            set { SetValue(BoundPasswordProperty, value); }
        }

        private bool _isPasswordChanging;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PasswordChanged += HandlePasswordChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PasswordChanged -= HandlePasswordChanged;
        }

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) 
        {
            if (d is PasswordBoxAssistant behavior && behavior.AssociatedObject != null && !behavior._isPasswordChanging)
            {
                behavior.UpdatePasswordBoxFromBoundPassword();
            }
        }

        private void UpdatePasswordBoxFromBoundPassword()
        {
            AssociatedObject.PasswordChanged -= HandlePasswordChanged;

            SecureString ss = BoundPassword;
            if (ss == null || ss.Length == 0)
            {
                AssociatedObject.Password = string.Empty;
            }

            // Re-attach handler
            AssociatedObject.PasswordChanged += HandlePasswordChanged;
        }
        private void HandlePasswordChanged(object sender, RoutedEventArgs e)
        {
            _isPasswordChanging = true;
            BoundPassword = AssociatedObject.SecurePassword;
            _isPasswordChanging = false;
        }
    }
}
