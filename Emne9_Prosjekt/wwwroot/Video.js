window.updateGameState = function (state) {
    const video = document.getElementById("bgVideo");
    const introEnd = 8.3;

    if (!video) return;

    if (state === "Playing") {
        video.currentTime = 0;
        video.play();

        const pauseAt = () => {
            if (video.currentTime >= introEnd) {
                video.pause();
                video.removeEventListener("timeupdate", pauseAt);
            }
        };

        video.addEventListener("timeupdate", pauseAt);
    }

    if (state === "GameOver") {
        video.currentTime = introEnd;
        video.play();
    }
};
