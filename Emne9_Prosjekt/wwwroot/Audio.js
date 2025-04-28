const soundCache = {};
window.playSound = function (sound) {
    if (!soundCache[sound]) {
        soundCache[sound] = new Audio(sound);
        soundCache[sound].volume = 0.1;
    }
    soundCache[sound].play();
};
