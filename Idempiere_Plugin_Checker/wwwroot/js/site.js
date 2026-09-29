$(function () {
    let activeAmbId = 'all';
    let searchQuery = '';

    const $cards = $('.plugin-card-wrapper');
    const $searchInput = $('#pluginSearchInput');
    const $clearBtn = $('#searchClearBtn');
    const $bubbles = $('.filter-bubble');
    const $visibleCounter = $('#visiblePluginsCount');
    const $noResultsAlert = $('#noResultsAlert');
    const $resetBtn = $('#resetFiltersBtn');

    // Inicializar ambiente activo según la selección inicial del modelo
    const $initialActive = $bubbles.filter('.active');
    if ($initialActive.length > 0) {
        activeAmbId = $initialActive.data('amb-id').toString();
    }

    // 1. Filtrado dinámico por globitos de ambientes
    $bubbles.on('click', function () {
        $bubbles.removeClass('active');
        $(this).addClass('active');

        activeAmbId = $(this).data('amb-id').toString();
        applyFilters();
    });

    // 2. Filtrado en tiempo real por barra de búsqueda
    $searchInput.on('input', function () {
        searchQuery = $(this).val().trim().toLowerCase();

        if (searchQuery.length > 0) {
            $clearBtn.css('display', 'inline-flex');
        } else {
            $clearBtn.hide();
        }

        applyFilters();
    });

    // Limpiar barra de búsqueda
    $clearBtn.on('click', function () {
        $searchInput.val('');
        searchQuery = '';
        $(this).hide();
        $searchInput.trigger('focus');
        applyFilters();
    });

    // Botón de restablecer filtros en caso de sin resultados
    $resetBtn.on('click', function () {
        $bubbles.removeClass('active');
        $bubbles.filter('[data-amb-id="all"]').addClass('active');
        activeAmbId = 'all';

        $searchInput.val('');
        searchQuery = '';
        $clearBtn.hide();

        applyFilters();
    });

    // Función unificada de filtrado
    function applyFilters() {
        let visibleCount = 0;

        $cards.each(function () {
            const $card = $(this);
            const cardAmbId = ($card.data('amb-id') || '').toString();
            const symbolic = ($card.data('symbolic') || '').toString();
            const name = ($card.data('name') || '').toString();
            const state = ($card.data('state') || '').toString();
            const author = ($card.data('author') || '').toString();
            const version = ($card.data('version') || '').toString();
            const ambName = ($card.data('ambname') || '').toString();

            // Validación por ambiente
            const matchesAmb = (activeAmbId === 'all' || cardAmbId === activeAmbId);

            // Validación por término de búsqueda
            let matchesSearch = true;
            if (searchQuery.length > 0) {
                matchesSearch = symbolic.includes(searchQuery) ||
                                name.includes(searchQuery) ||
                                state.includes(searchQuery) ||
                                author.includes(searchQuery) ||
                                version.includes(searchQuery) ||
                                ambName.includes(searchQuery);
            }

            if (matchesAmb && matchesSearch) {
                $card.stop(true, true).fadeIn(180);
                visibleCount++;
            } else {
                $card.stop(true, true).hide();
            }
        });

        // Actualizar contador visual
        $visibleCounter.text(visibleCount);

        // Mostrar u ocultar estado vacío
        if (visibleCount === 0) {
            $noResultsAlert.fadeIn(180);
        } else {
            $noResultsAlert.hide();
        }
    }
});

