window.onload = function () {
    console.log('Loaded');
    const filter = document.getElementById('filter');

    console.log('Found filter component: ', filter)
    filter.addEventListener('change', () => {
        console.log(filter.enabledLogLevels);
    });
};