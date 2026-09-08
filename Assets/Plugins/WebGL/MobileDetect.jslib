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
  }
});
