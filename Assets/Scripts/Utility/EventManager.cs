using System.ComponentModel.Design;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

namespace Events
{
    public class Disposer<T> : IDisposable
    {
        private EventManager _eventManager;
        private Action<T> _listener;

        public Disposer(EventManager em, Action<T> listener) {
            _eventManager = em;
            _listener = listener;
        }

        public void Dispose()
        {
            _eventManager.RemoveListener<T>(_listener);
        }
    }
    
    public class EventManager
    {
        private ServiceContainer _serviceContainer;
        private IList<Action> _pendingActions;
        private int _isDispatching = 0;

        public EventManager() {
            _serviceContainer = new ServiceContainer();
            _pendingActions = new List<Action>();
        }

        private IList<Action<T>> GetListenerList<T>() {
            return _serviceContainer.GetService(typeof(IList<Action<T>>)) as IList<Action<T>>;   
        }

        public Disposer<T> AddListener<T>(Action<T> action) {
            var disposer = new Disposer<T>(this, action);

            var listenerList = GetListenerList<T>();
            if (listenerList == null) {
                _serviceContainer.AddService(typeof(IList<Action<T>>), new List<Action<T>>());
                listenerList = GetListenerList<T>();
            }

            if (_isDispatching > 0) {
                _pendingActions.Add(() => listenerList.Add(action));
            } else {
                listenerList.Add(action);
            }

            return disposer;
        }

        public void RemoveListener<T>(Action<T> action) {
            var listenerList = GetListenerList<T>();
            if (listenerList != null) {
                if (_isDispatching > 0) {
                    _pendingActions.Add(() => listenerList.Remove(action));
                } else {
                    listenerList.Remove(action);
                }
            }
        }

        private void RunPendingActions() {
            foreach (var action in _pendingActions) {
                action.Invoke();
            }

            _pendingActions.Clear();
        }

        public bool Dispatch<T>() {
            return Dispatch<T>(default(T));
        }

        public bool Dispatch<T>(T eventData) {
            var listenerList = GetListenerList<T>();

            if (_isDispatching == 0) {
                RunPendingActions();
            }

            if (listenerList != null && listenerList.Count > 0) {

                _isDispatching++;
                foreach (var listener in listenerList)
                {
                    listener.Invoke(eventData);
                }
                _isDispatching--;
                
                return true;
            }

            return false;
        }
    }
}