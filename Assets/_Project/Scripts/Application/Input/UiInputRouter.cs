using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace AnoGame.Application.Input
{
    public sealed class UiInputRouter : MonoBehaviour
    {
        private readonly Stack<Func<bool>> _cancelStack = new();
        private InputAction _cancelAction;

        public void BeginRouting(InputAction cancelAction)
        {
            // UI表示時のみ呼ばれる想定（Inventory.Show 等）
            EndRouting(); // 二重購読ガード
            _cancelAction = cancelAction;
            _cancelAction.performed += OnCancel;
        }

        public void EndRouting()
        {
            if (_cancelAction != null)
            {
                _cancelAction.performed -= OnCancel;
                _cancelAction = null;
            }
            _cancelStack.Clear();
        }

        public IDisposable PushCancelHandler(Func<bool> handler)
        {
            _cancelStack.Push(handler);
            return new PopToken(this, handler);
        }

        public event Action OnUnhandledCancel;

        private void OnCancel(InputAction.CallbackContext _)
        {
            if (_cancelStack.Count > 0 && _cancelStack.Peek().Invoke())
                return;

            OnUnhandledCancel?.Invoke();
        }

        private void Pop(Func<bool> handler)
        {
            if (_cancelStack.Count == 0) return;
            if (ReferenceEquals(_cancelStack.Peek(), handler)) { _cancelStack.Pop(); return; }

            var tmp = new Stack<Func<bool>>();
            while (_cancelStack.Count > 0)
            {
                var h = _cancelStack.Pop();
                if (ReferenceEquals(h, handler)) break;
                tmp.Push(h);
            }
            while (tmp.Count > 0) _cancelStack.Push(tmp.Pop());
        }

        private sealed class PopToken : IDisposable
        {
            private UiInputRouter _owner; private Func<bool> _handler;
            public PopToken(UiInputRouter owner, Func<bool> handler) { _owner = owner; _handler = handler; }
            public void Dispose() { _owner?.Pop(_handler); _owner = null; _handler = null; }
        }
    }
}