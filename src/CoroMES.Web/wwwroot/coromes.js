window.coromes = {
    requestFullscreen: () => {
        const target = document.documentElement;
        if (target.requestFullscreen) {
            return target.requestFullscreen();
        }
    }
};
