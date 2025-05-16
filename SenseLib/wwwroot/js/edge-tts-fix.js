/**
 * Edge TTS Fix
 * Sửa lỗi và cải thiện chức năng TTS
 */

document.addEventListener('DOMContentLoaded', function () {
    // Kiểm tra xem đối tượng edgeTTS có tồn tại không
    if (typeof edgeTTS === 'undefined') {
        console.error("Không tìm thấy đối tượng edgeTTS. Đảm bảo đã tải edge-tts.js trước.");
        return;
    }

    // Lấy các phần tử nút TTS
    const ttsPlayBtn = document.getElementById('tts-play');
    const ttsPauseBtn = document.getElementById('tts-pause');
    const ttsStopBtn = document.getElementById('tts-stop');

    if (!ttsPlayBtn) {
        console.error("Không tìm thấy nút TTS Play. Kiểm tra HTML.");
        return;
    }

    // Thêm thanh tiến độ và thông báo trạng thái vào giao diện
    addProgressBar();
    addStatusMessage();

    // Cải thiện giao diện các nút điều khiển
    enhanceControlButtons();

    // Thiết lập các callback của edgeTTS
    setupTTSCallbacks();

    // Xử lý sự kiện nút Đọc
    ttsPlayBtn.addEventListener('click', function () {
        console.log("Nút Đọc được nhấp");

        // Lấy văn bản để đọc
        extractTextFromCurrentDocument().then(text => {
            if (!text || text.trim() === '') {
                showStatusMessage('Không thể trích xuất văn bản để đọc. Tài liệu có thể là hình ảnh hoặc không có lớp văn bản.', 'error');
                return;
            }

            console.log("Đã trích xuất văn bản, độ dài:", text.length, "ký tự");

            // Hiển thị thanh tiến độ
            showProgressBar();
            showStatusMessage('Bắt đầu đọc văn bản...', 'info');

            // Thiết lập kích thước đoạn phù hợp
            if (text.length > 5000) {
                edgeTTS.setChunkSize(800); // Đoạn dài hơn cho văn bản lớn
            } else if (text.length > 2000) {
                edgeTTS.setChunkSize(500); // Đoạn trung bình
            } else {
                edgeTTS.setChunkSize(300); // Đoạn ngắn cho văn bản ngắn
            }

            // Bắt đầu đọc văn bản
            edgeTTS.speak(text);

            // Hiển thị nút tạm dừng và dừng, ẩn nút play
            ttsPlayBtn.style.display = 'none';
            ttsPauseBtn.style.display = 'inline-block';
            ttsStopBtn.style.display = 'inline-block';
        }).catch(error => {
            console.error("Lỗi khi trích xuất văn bản:", error);
            showStatusMessage('Có lỗi khi trích xuất văn bản: ' + error.message, 'error');
        });
    });

    // Thiết lập các callback của edgeTTS
    function setupTTSCallbacks() {
        // Theo dõi tiến độ
        edgeTTS.registerProgressCallback(updateProgressBar);

        // Xử lý lỗi
        edgeTTS.registerErrorCallback(function (errorMessage) {
            showStatusMessage(errorMessage, 'warning');
        });

        // Xử lý khi hoàn thành
        edgeTTS.registerCompleteCallback(function () {
            showStatusMessage('Đã đọc xong toàn bộ văn bản.', 'success');

            // Hiển thị lại nút Play, ẩn nút Pause và Stop
            ttsPlayBtn.style.display = 'inline-block';
            ttsPauseBtn.style.display = 'none';
            ttsStopBtn.style.display = 'none';

            // Đặt lại nút tạm dừng
            resetPauseButton();

            // Ẩn thanh tiến độ sau 3 giây
            setTimeout(() => {
                hideProgressBar();
            }, 3000);
        });

        // Xử lý khi bắt đầu
        edgeTTS.registerStartCallback(function (textLength) {
            showStatusMessage(`Bắt đầu đọc văn bản dài ${textLength} ký tự...`, 'info');
        });
    }

    // Hàm để trích xuất văn bản từ tài liệu hiện tại
    async function extractTextFromCurrentDocument() {
        return new Promise((resolve, reject) => {
            try {
                // Lấy thông tin về tài liệu
                const docxViewer = document.getElementById('docx-viewer');
                const pdfViewer = document.getElementById('pdf-viewer');
                const isDocx = docxViewer && docxViewer.style.display !== 'none';

                // Lấy số trang hiện tại
                const currentPageEl = document.getElementById('current-page');
                const currentPage = currentPageEl ? parseInt(currentPageEl.textContent) : 1;

                let textContent = '';

                if (isDocx && docxViewer) {
                    // Trích xuất từ DOCX
                    textContent = extractAllText(docxViewer);
                    resolve(textContent);
                } else {
                    // Trích xuất từ PDF
                    const iframe = document.querySelector('#pdf-viewer iframe');
                    if (!iframe) {
                        reject(new Error("Không tìm thấy iframe của PDF"));
                        return;
                    }

                    // Đợi iframe load xong (nếu cần)
                    if (iframe.contentDocument.readyState !== 'complete') {
                        iframe.onload = extractFromPDF;
                    } else {
                        extractFromPDF();
                    }

                    function extractFromPDF() {
                        try {
                            const iframeDoc = iframe.contentDocument || iframe.contentWindow.document;

                            // Phương pháp 1: Tìm .textLayer
                            const textLayers = iframeDoc.querySelectorAll('.textLayer');
                            if (textLayers.length > 0 && textLayers[currentPage - 1]) {
                                textContent = extractAllText(textLayers[currentPage - 1]);
                            }

                            // Phương pháp 2: Tìm theo data-page-number
                            if (!textContent) {
                                const pageDiv = iframeDoc.querySelector(`.page[data-page-number="${currentPage}"]`);
                                if (pageDiv) {
                                    const textDivs = pageDiv.querySelectorAll('.textLayer div');
                                    for (const div of textDivs) {
                                        textContent += ' ' + div.textContent;
                                    }
                                }
                            }

                            // Phương pháp 3: Tìm tất cả các div trong trang hiện tại
                            if (!textContent) {
                                const allTextDivs = iframeDoc.querySelectorAll('div');
                                let pageContent = '';
                                for (const div of allTextDivs) {
                                    if (div.textContent && div.textContent.trim()) {
                                        pageContent += ' ' + div.textContent;
                                    }
                                }
                                textContent = pageContent;
                            }

                            // Phương pháp 4: Thử lấy text từ PDF.js
                            if (!textContent && iframeDoc.PDFViewerApplication) {
                                try {
                                    const pdfViewer = iframeDoc.PDFViewerApplication.pdfViewer;
                                    if (pdfViewer && pdfViewer.getPageView) {
                                        const pageView = pdfViewer.getPageView(currentPage - 1);
                                        if (pageView && pageView.textLayer && pageView.textLayer.textContentItemsStr) {
                                            textContent = pageView.textLayer.textContentItemsStr.join(' ');
                                        }
                                    }
                                } catch (pdfError) {
                                    console.warn("Lỗi khi trích xuất văn bản từ PDF.js:", pdfError);
                                }
                            }

                            // Phương pháp 5: Nếu tất cả thất bại, thử lấy toàn bộ nội dung văn bản
                            if (!textContent) {
                                textContent = iframeDoc.body.innerText;
                            }

                            // Làm sạch văn bản
                            textContent = cleanupText(textContent);

                            resolve(textContent);
                        } catch (error) {
                            console.error("Lỗi khi trích xuất PDF:", error);
                            reject(error);
                        }
                    }
                }
            } catch (error) {
                reject(error);
            }
        });
    }

    // Hàm trích xuất văn bản từ phần tử HTML
    function extractAllText(element) {
        if (!element) return '';
        return EdgeTTS.extractTextFromElement(element);
    }

    // Làm sạch văn bản
    function cleanupText(text) {
        if (!text) return '';

        // Loại bỏ khoảng trắng thừa
        text = text.replace(/\s+/g, ' ');

        // Xóa các ký tự đặc biệt không cần thiết
        text = text.replace(/[^\p{L}\p{N}\p{P}\s]/gu, '');

        // Đảm bảo có dấu chấm và khoảng trắng sau mỗi dấu chấm
        text = text.replace(/\./g, '. ');
        text = text.replace(/\s\s+/g, ' ');

        // Chuẩn hóa dấu xuống dòng
        text = text.replace(/[\r\n]+/g, '\n');

        return text.trim();
    }

    // Thêm thanh tiến độ vào giao diện
    function addProgressBar() {
        const ttsControls = document.querySelector('.tts-controls');
        if (ttsControls) {
            // Tạo container cho thanh tiến độ
            const progressContainer = document.createElement('div');
            progressContainer.id = 'tts-progress-container';
            progressContainer.style.display = 'none';
            progressContainer.style.width = '100%';
            progressContainer.style.height = '5px';
            progressContainer.style.backgroundColor = '#ddd';
            progressContainer.style.borderRadius = '2px';
            progressContainer.style.margin = '5px 0';
            progressContainer.style.position = 'relative';

            // Tạo thanh tiến độ
            const progressBar = document.createElement('div');
            progressBar.id = 'tts-progress-bar';
            progressBar.style.width = '0%';
            progressBar.style.height = '100%';
            progressBar.style.backgroundColor = '#4CAF50';
            progressBar.style.borderRadius = '2px';
            progressBar.style.transition = 'width 0.3s';

            // Tạo nhãn hiển thị phần trăm
            const progressLabel = document.createElement('div');
            progressLabel.id = 'tts-progress-label';
            progressLabel.style.position = 'absolute';
            progressLabel.style.right = '0';
            progressLabel.style.top = '-15px';
            progressLabel.style.fontSize = '10px';
            progressLabel.style.color = '#333';
            progressLabel.textContent = '0%';

            // Thêm thanh tiến độ vào container
            progressContainer.appendChild(progressBar);
            progressContainer.appendChild(progressLabel);

            // Thêm container tiến độ vào sau các nút điều khiển
            const readerControls = ttsControls.closest('.reader-controls');
            if (readerControls) {
                readerControls.appendChild(progressContainer);
            } else {
                ttsControls.parentNode.insertBefore(progressContainer, ttsControls.nextSibling);
            }
        }
    }

    // Thêm phần tử hiển thị thông báo trạng thái
    function addStatusMessage() {
        const ttsControls = document.querySelector('.tts-controls');
        if (ttsControls) {
            // Tạo container cho thông báo trạng thái
            const statusContainer = document.createElement('div');
            statusContainer.id = 'tts-status-container';
            statusContainer.style.display = 'none';
            statusContainer.style.width = '100%';
            statusContainer.style.padding = '5px 10px';
            statusContainer.style.marginTop = '5px';
            statusContainer.style.borderRadius = '4px';
            statusContainer.style.fontSize = '12px';
            statusContainer.style.transition = 'background-color 0.3s';

            // Thêm container trạng thái vào sau thanh tiến độ
            const readerControls = ttsControls.closest('.reader-controls');
            if (readerControls) {
                readerControls.appendChild(statusContainer);
            } else {
                const progressContainer = document.getElementById('tts-progress-container');
                if (progressContainer) {
                    ttsControls.parentNode.insertBefore(statusContainer, progressContainer.nextSibling);
                } else {
                    ttsControls.parentNode.insertBefore(statusContainer, ttsControls.nextSibling);
                }
            }
        }
    }

    // Hiển thị thông báo trạng thái
    function showStatusMessage(message, type = 'info') {
        const statusContainer = document.getElementById('tts-status-container');
        if (!statusContainer) return;

        // Đặt kiểu dựa trên loại thông báo
        statusContainer.style.display = 'block';
        statusContainer.textContent = message;

        // Đặt màu nền dựa trên loại
        switch (type) {
            case 'error':
                statusContainer.style.backgroundColor = '#ffebee';
                statusContainer.style.color = '#c62828';
                statusContainer.style.border = '1px solid #ffcdd2';
                break;
            case 'warning':
                statusContainer.style.backgroundColor = '#fff8e1';
                statusContainer.style.color = '#ff8f00';
                statusContainer.style.border = '1px solid #ffe0b2';
                break;
            case 'success':
                statusContainer.style.backgroundColor = '#e8f5e9';
                statusContainer.style.color = '#2e7d32';
                statusContainer.style.border = '1px solid #c8e6c9';
                break;
            case 'info':
            default:
                statusContainer.style.backgroundColor = '#e3f2fd';
                statusContainer.style.color = '#1565c0';
                statusContainer.style.border = '1px solid #bbdefb';
        }

        // Tự động ẩn thông báo sau 5 giây cho các thông báo thành công
        if (type === 'success') {
            setTimeout(() => {
                statusContainer.style.display = 'none';
            }, 5000);
        }
    }

    // Hiển thị thanh tiến độ
    function showProgressBar() {
        const progressContainer = document.getElementById('tts-progress-container');
        if (progressContainer) {
            progressContainer.style.display = 'block';
        }
    }

    // Ẩn thanh tiến độ
    function hideProgressBar() {
        const progressContainer = document.getElementById('tts-progress-container');
        if (progressContainer) {
            progressContainer.style.display = 'none';
        }
    }

    // Cập nhật thanh tiến độ
    function updateProgressBar(percentage) {
        const progressBar = document.getElementById('tts-progress-bar');
        const progressLabel = document.getElementById('tts-progress-label');

        if (progressBar && progressLabel) {
            progressBar.style.width = `${percentage}%`;
            progressLabel.textContent = `${percentage}%`;
        }
    }

    // Đặt lại nút tạm dừng
    function resetPauseButton() {
        const ttsPauseBtn = document.getElementById('tts-pause');
        if (ttsPauseBtn) {
            const icon = ttsPauseBtn.querySelector('i');
            if (icon) {
                // Đảm bảo icon là fa-pause
                icon.className = ''; // Xóa tất cả class
                icon.classList.add('fas', 'fa-pause');
            }

            // Đặt lại nội dung văn bản và tiêu đề
            ttsPauseBtn.innerHTML = '<i class="fas fa-pause"></i> Tạm dừng';
            ttsPauseBtn.title = "Tạm dừng đọc";
        }
    }

    // Xử lý sự kiện nút Tạm dừng
    ttsPauseBtn.addEventListener('click', function () {
        const icon = this.querySelector('i');

        if (icon.classList.contains('fa-pause')) {
            // Đang phát -> tạm dừng
            edgeTTS.pause();

            // Cập nhật giao diện nút
            icon.className = ''; // Xóa tất cả class
            icon.classList.add('fas', 'fa-play');
            this.innerHTML = '<i class="fas fa-play"></i> Tiếp tục';
            this.title = "Tiếp tục đọc";

            // Thay đổi màu nút
            this.style.backgroundColor = "#4CAF50"; // Màu xanh lá

            showStatusMessage('Đã tạm dừng đọc.', 'info');
        } else {
            // Đang tạm dừng -> tiếp tục
            edgeTTS.resume();

            // Cập nhật giao diện nút
            icon.className = ''; // Xóa tất cả class
            icon.classList.add('fas', 'fa-pause');
            this.innerHTML = '<i class="fas fa-pause"></i> Tạm dừng';
            this.title = "Tạm dừng đọc";

            // Khôi phục màu nút
            this.style.backgroundColor = "";

            showStatusMessage('Đang tiếp tục đọc...', 'info');
        }
    });

    // Xử lý sự kiện nút Dừng
    ttsStopBtn.addEventListener('click', function () {
        edgeTTS.stop();

        // Hiệu ứng nhấp nháy nút khi dừng
        this.classList.add('stop-button-flash');
        setTimeout(() => {
            this.classList.remove('stop-button-flash');
        }, 300);

        // Hiển thị lại nút play, ẩn nút tạm dừng và dừng
        ttsPlayBtn.style.display = 'inline-block';
        ttsPauseBtn.style.display = 'none';
        ttsStopBtn.style.display = 'none';

        // Đặt lại nút tạm dừng
        resetPauseButton();

        // Ẩn thanh tiến độ và hiển thị thông báo
        hideProgressBar();
        showStatusMessage('Đã dừng đọc.', 'info');
    });

    // Thêm style cho hiệu ứng nhấp nháy nút dừng
    const style = document.createElement('style');
    style.textContent = `
        .stop-button-flash {
            animation: stopButtonFlash 0.3s ease;
        }
        
        @keyframes stopButtonFlash {
            0%, 100% { background-color: #e53935; }
            50% { background-color: #f44336; }
        }
        
        .btn-control {
            transition: all 0.2s ease;
        }
    `;
    document.head.appendChild(style);

    // Thêm nút Debug nếu cần
    if (window.location.search.includes('debug=true')) {
        addDebugButton();
    }

    // Thêm nút debug
    function addDebugButton() {
        const ttsControls = document.querySelector('.tts-controls');
        if (ttsControls) {
            const debugBtn = document.createElement('button');
            debugBtn.className = 'btn-control';
            debugBtn.id = 'tts-debug';
            debugBtn.innerHTML = '<i class="fas fa-bug"></i> Test TTS';
            debugBtn.style.backgroundColor = '#ff9800';

            ttsControls.appendChild(debugBtn);

            debugBtn.addEventListener('click', testTTS);
        }
    }

    // Hàm kiểm tra TTS
    async function testTTS() {
        try {
            showStatusMessage('Đang kiểm tra TTS...', 'info');

            const response = await fetch('/api/tts/test');
            const data = await response.json();

            if (data.success) {
                showStatusMessage('Kiểm tra TTS thành công!', 'success');
                console.log('TTS Test Success:', data);

                // Phát audio kiểm tra
                const audio = new Audio(data.audioUrl);
                audio.play();
            } else {
                showStatusMessage(`Kiểm tra TTS thất bại: ${data.error}`, 'error');
                console.error('TTS Test Failed:', data);
            }
        } catch (error) {
            showStatusMessage(`Lỗi kiểm tra TTS: ${error.message}`, 'error');
            console.error('TTS Test Error:', error);
        }
    }

    // Cải thiện giao diện các nút điều khiển
    function enhanceControlButtons() {
        // Cải thiện nút play
        if (ttsPlayBtn) {
            ttsPlayBtn.title = "Bắt đầu đọc văn bản";
            applyButtonHoverEffect(ttsPlayBtn);
        }

        // Cải thiện nút tạm dừng
        if (ttsPauseBtn) {
            ttsPauseBtn.title = "Tạm dừng đọc";
            applyButtonHoverEffect(ttsPauseBtn);
        }

        // Cải thiện nút dừng
        if (ttsStopBtn) {
            ttsStopBtn.title = "Dừng đọc hoàn toàn";
            ttsStopBtn.style.backgroundColor = "#e53935"; // Màu đỏ nhẹ cho nút dừng
            applyButtonHoverEffect(ttsStopBtn);
        }
    }

    // Áp dụng hiệu ứng hover cho nút
    function applyButtonHoverEffect(button) {
        button.addEventListener('mouseenter', function () {
            this.style.transform = 'scale(1.05)';
            this.style.transition = 'all 0.2s ease';
        });

        button.addEventListener('mouseleave', function () {
            this.style.transform = 'scale(1)';
        });
    }

    console.log("Edge TTS Fix đã được tải");
}); 