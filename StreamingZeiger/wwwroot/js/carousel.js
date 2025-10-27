const fadeElements = document.querySelectorAll('.fade-in');
const observer = new IntersectionObserver(entries => {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            entry.target.classList.add('visible');
        }
    });
}, { threshold: 0.1 });
fadeElements.forEach(el => observer.observe(el));

const carousel = document.getElementById('movieCarousel');
const carouselItems = carousel.querySelectorAll('.carousel-item img');

function animateCarousel() {
    carouselItems.forEach((img, idx) => {
        if (img.closest('.carousel-item').classList.contains('active')) {
            img.style.opacity = 1;
            img.style.transform = 'scale(1)';
        } else {
            img.style.opacity = 0;
            img.style.transform = 'scale(0.95)';
        }
    });
}

animateCarousel();

carousel.addEventListener('slid.bs.carousel', animateCarousel);