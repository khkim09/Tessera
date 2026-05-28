using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tessera.Runtime
{
    /// <summary>
    /// Tessera 런타임 전역 이벤트를 안전하게 중계하는 정적 이벤트 버스다.
    /// Unity 오브젝트 생성 순서나 씬 전환 순서에 의존하지 않는다.
    /// </summary>
    public static class TesseraEventBus
    {
        private static readonly Dictionary<Type, Delegate> handlersByType = new Dictionary<Type, Delegate>();

        /// <summary>지정 이벤트 타입을 구독하고 해제용 토큰을 반환한다.</summary>
        public static IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
                return EventSubscription.Empty;

            Type eventType = typeof(TEvent);

            if (handlersByType.TryGetValue(eventType, out Delegate existingHandler))
                handlersByType[eventType] = Delegate.Combine(existingHandler, handler);
            else
                handlersByType.Add(eventType, handler);

            return new EventSubscription(() => Unsubscribe(handler));
        }

        /// <summary>지정 이벤트 타입 구독을 해제한다.</summary>
        public static void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
                return;

            Type eventType = typeof(TEvent);

            if (!handlersByType.TryGetValue(eventType, out Delegate existingHandler))
                return;

            Delegate nextHandler = Delegate.Remove(existingHandler, handler);

            if (nextHandler == null)
                handlersByType.Remove(eventType);
            else
                handlersByType[eventType] = nextHandler;
        }

        /// <summary>이벤트를 발행한다. 구독자가 없어도 실패하지 않는다.</summary>
        public static void Publish<TEvent>(TEvent gameEvent)
        {
            Type eventType = typeof(TEvent);

            if (!handlersByType.TryGetValue(eventType, out Delegate existingHandler))
                return;

            Delegate[] invocationList = existingHandler.GetInvocationList();

            for (int i = 0; i < invocationList.Length; i++)
            {
                if (invocationList[i] is not Action<TEvent> typedHandler)
                    continue;

                try
                {
                    typedHandler.Invoke(gameEvent);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        /// <summary>씬 재시작 또는 테스트 초기화 시 모든 구독을 제거한다.</summary>
        public static void ClearAll()
        {
            handlersByType.Clear();
        }

        /// <summary>이벤트 구독 해제용 토큰이다.</summary>
        private sealed class EventSubscription : IDisposable
        {
            public static readonly EventSubscription Empty = new EventSubscription(null);

            private Action disposeAction;
            private bool isDisposed;

            public EventSubscription(Action disposeAction)
            {
                this.disposeAction = disposeAction;
            }

            public void Dispose()
            {
                if (isDisposed)
                    return;

                isDisposed = true;
                disposeAction?.Invoke();
                disposeAction = null;
            }
        }
    }
}
