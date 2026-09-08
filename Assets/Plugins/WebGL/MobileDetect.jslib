mergeInto(LibraryManager.library, {
  AetherionIsMobile: function () {
    var ua = (typeof navigator !== "undefined" && navigator.userAgent) ? navigator.userAgent : "";
    var mobileUa = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini|Mobile|Silk|Tablet/i.test(ua);
    var touchPoints = (typeof navigator !== "undefined" && navigator.maxTouchPoints) ? navigator.maxTouchPoints : 0;
    var coarse = false;
    try {
      coarse = !!(window.matchMedia && window.matchMedia("(pointer: coarse)").matches);
    } catch (e) {}
    var small = false;
    try {
      small = Math.min(screen.width, screen.height) <= 920;
    } catch (e) {}
    return (mobileUa || (touchPoints > 0 && (coarse || small))) ? 1 : 0;
  },

  AetherionSafeInsetBottom: function () {
    var css = 0;
    try {
      var probe = document.createElement("div");
      probe.style.paddingBottom = "env(safe-area-inset-bottom)";
      probe.style.position = "absolute";
      document.body.appendChild(probe);
      css = parseFloat(window.getComputedStyle(probe).paddingBottom) || 0;
      document.body.removeChild(probe);
    } catch (e) {}
    var chrome = 0;
    try {
      if (window.visualViewport) {
        chrome = Math.max(0, window.innerHeight - window.visualViewport.height - window.visualViewport.offsetTop);
      }
    } catch (e) {}
    return Math.max(css, chrome);
  }
});
