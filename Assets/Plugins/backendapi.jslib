mergeInto(LibraryManager.library, {
  
  SendWalletAddressReady: function() {
	dispatchReactUnityEvent('ReadyToWalletAddress')
  },
  
  SendGameOver: function() {
	dispatchReactUnityEvent('GameOver')
  },
  
});
