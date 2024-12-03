let el;
const distance = 68;

function scrollNavbarShadow() {
if (this.pageYOffset > distance) {
    el = document.getElementById('navbar');
    el?.classList?.remove('navbar-transparent');
    el?.classList?.add('bg-dark');
} else {
        el = document.getElementById('navbar');
        el?.classList?.add('navbar-transparent');
        el?.classList?.remove( 'bg-dark');
}
}

    window.addEventListener("scroll", function() {
    scrollNavbarShadow();
    
});

    if (document.getElementsByClassName('page-header')) {
    window.addEventListener('scroll', function() {
        var scrollPosition = window.pageYOffset;
        var bgParallax = document.querySelector('.page-header');
        var limit = bgParallax.offsetTop + bgParallax.offsetHeight;
        if (scrollPosition > bgParallax.offsetTop && scrollPosition <= limit) {
            bgParallax.style.backgroundPositionY = (50 - 10 * scrollPosition / limit * 3) + '%';
        } else {
            bgParallax.style.backgroundPositionY = '50%';
        }
    });
}