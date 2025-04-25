const soundCache = {};
window.playSound = function (sound) {
    if (!soundCache[sound]) {
        soundCache[sound] = new Audio(sound);
    }
    soundCache[sound].play();
};
