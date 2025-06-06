function initializeCharts(tipoData, locazioneData, giornoData) {
    if (document.getElementById('tipoChart')) {
        new Chart(document.getElementById('tipoChart'), {
            type: 'pie',
            data: tipoData,
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: window.innerWidth < 768 ? 'bottom' : 'right',
                        labels: {
                            boxWidth: 15,
                            font: {
                                size: window.innerWidth < 768 ? 10 : 12
                            }
                        }
                    }
                }
            }
        });
    }

    if (document.getElementById('locazioneChart')) {
        new Chart(document.getElementById('locazioneChart'), {
            type: 'pie',
            data: locazioneData,
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: window.innerWidth < 768 ? 'bottom' : 'right',
                        labels: {
                            boxWidth: 15,
                            font: {
                                size: window.innerWidth < 768 ? 10 : 12
                            }
                        }
                    }
                }
            }
        });
    }

    if (document.getElementById('giornoChart')) {
        new Chart(document.getElementById('giornoChart'), {
            type: 'line',
            data: giornoData,
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: { stepSize: 1 }
                    }
                }
            }
        });
    }
}

$(document).ready(function () {
    // Initialize Select2
    $('.select2').select2({
        theme: 'bootstrap-5',
        width: '100%'
    });

    // Gestione click sulle card dei comuni
    $('.comune-card').on('click', function() {
        var comune = $(this).data('comune');
        $('#selectedComune').val(comune).trigger('change');
        $('#filterForm').submit();
    });

    function fixInvalidDate(input) {
        if (!input.value) return;

        const parts = input.value.split("-");
        if (parts.length !== 3) return;

        let year = parseInt(parts[0], 10);
        const currentYear = new Date().getFullYear();

        if (isNaN(year) || year < 2020) {
            parts[0] = currentYear;
            input.value = parts.join("-");
        }
    }

    $('#dateFrom, #dateTo').on('change blur', function () {
        fixInvalidDate(this);
    });
}); 