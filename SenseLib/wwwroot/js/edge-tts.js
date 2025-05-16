/**
 * Edge TTS Frontend Module
 * Quản lý việc chuyển đổi văn bản thành giọng nói sử dụng Edge TTS
 */

class EdgeTTS {
    constructor() {
        this.isPlaying = false;
        this.isPaused = false;
        this.audio = new Audio();
        this.queue = [];
        this.currentText = '';
        this.defaultVoice = 'vi-VN-HoaiMyNeural';
        this.defaultRate = '+0%';
        this.defaultVolume = '+0%';

        // Kích thước mỗi đoạn văn bản (số ký tự)
        this.chunkSize = 800;

        // Ngưỡng dừng khi phân đoạn câu
        this.sentenceBreakThreshold = 150;

        // Văn bản đầy đủ đang được đọc và vị trí hiện tại
        this.fullText = '';
        this.currentPosition = 0;

        // Phần trăm đã hoàn thành
        this.completionPercentage = 0;

        // Biến đánh dấu lỗi
        this.hasError = false;
        this.errorMessage = '';

        // Thời gian chờ tối đa (ms) khi gọi API
        this.apiTimeout = 15000;

        // Sự kiện callback
        this.onProgressChange = null;
        this.onError = null;
        this.onComplete = null;
        this.onStart = null;

        // Xử lý sự kiện kết thúc audio
        this.audio.addEventListener('ended', () => {
            this.isPlaying = false;
            // Kiểm tra xem đã đọc hết văn bản chưa
            if (this.currentPosition < this.fullText.length) {
                // Nếu chưa đọc hết, tạo đoạn tiếp theo và thêm vào hàng đợi
                this.processNextChunk();
            } else if (this.queue.length === 0) {
                // Nếu đã đọc hết văn bản và hàng đợi trống, thông báo hoàn thành
                console.log('Đã đọc xong toàn bộ văn bản');
                this.completionPercentage = 100;
                if (this.onProgressChange) {
                    this.onProgressChange(100);
                }
                if (this.onComplete) {
                    this.onComplete();
                }
            }
            this.playNext();
        });

        // Xử lý lỗi audio
        this.audio.addEventListener('error', (e) => {
            console.error('Lỗi phát audio:', e);
            this.hasError = true;
            this.errorMessage = 'Không thể phát audio';
            if (this.onError) {
                this.onError(this.errorMessage);
            }
        });
    }

    /**
     * Thêm văn bản vào hàng đợi để đọc
     * @param {string} text - Văn bản để đọc
     */
    speak(text) {
        if (!text || text.trim() === '') return;

        // Lưu văn bản đầy đủ và thiết lập lại vị trí hiện tại
        this.fullText = text;
        this.currentPosition = 0;
        this.completionPercentage = 0;
        this.hasError = false;
        this.errorMessage = '';

        // Thông báo bắt đầu
        if (this.onStart) {
            this.onStart(text.length);
        }

        // Xóa hàng đợi hiện tại
        this.queue = [];

        // Tính toán kích thước đoạn phù hợp dựa trên độ dài văn bản
        this.optimizeChunkSize(text.length);

        // Xử lý đoạn đầu tiên
        this.processNextChunk();

        // Nếu không có gì đang phát, bắt đầu phát
        if (!this.isPlaying && !this.isPaused) {
            this.playNext();
        }
    }

    /**
     * Tối ưu kích thước đoạn dựa trên độ dài văn bản
     * @param {number} textLength - Độ dài văn bản
     */
    optimizeChunkSize(textLength) {
        if (textLength > 10000) {
            this.chunkSize = 1000; // Đoạn lớn hơn cho văn bản rất dài
        } else if (textLength > 5000) {
            this.chunkSize = 800; // Đoạn lớn cho văn bản dài
        } else if (textLength > 2000) {
            this.chunkSize = 500; // Đoạn trung bình
        } else {
            this.chunkSize = 300; // Đoạn nhỏ cho văn bản ngắn
        }
        console.log(`Đã tối ưu kích thước đoạn thành ${this.chunkSize} cho văn bản dài ${textLength} ký tự`);
    }

    /**
     * Xử lý và thêm đoạn văn bản tiếp theo vào hàng đợi
     */
    processNextChunk() {
        if (this.currentPosition >= this.fullText.length) return;

        let chunk = '';
        let endPosition = Math.min(this.currentPosition + this.chunkSize, this.fullText.length);

        // Nếu còn đủ văn bản, tìm điểm kết thúc câu phù hợp
        if (endPosition < this.fullText.length) {
            // Tìm dấu chấm, chấm hỏi, hoặc chấm than trước
            let breakPoint = this.findSentenceBreak(this.fullText, this.currentPosition, endPosition);

            // Nếu tìm được điểm dừng phù hợp, sử dụng nó
            if (breakPoint > this.currentPosition) {
                endPosition = breakPoint + 1; // +1 để bao gồm cả dấu kết thúc câu
            }
        }

        // Trích đoạn văn bản
        chunk = this.fullText.substring(this.currentPosition, endPosition);

        // Cập nhật vị trí hiện tại
        this.currentPosition = endPosition;

        // Cập nhật phần trăm hoàn thành
        this.completionPercentage = Math.floor((this.currentPosition / this.fullText.length) * 100);
        if (this.onProgressChange) {
            this.onProgressChange(this.completionPercentage);
        }

        // Thêm đoạn vào hàng đợi nếu không rỗng
        if (chunk.trim() !== '') {
            this.queue.push(chunk);
        }
    }

    /**
     * Tìm vị trí kết thúc câu phù hợp để chia đoạn
     * @param {string} text - Văn bản đầy đủ
     * @param {number} start - Vị trí bắt đầu
     * @param {number} end - Vị trí kết thúc mặc định
     * @returns {number} - Vị trí kết thúc được điều chỉnh
     */
    findSentenceBreak(text, start, end) {
        // Các ký tự kết thúc câu
        const endMarks = ['.', '!', '?', ':', ';', '\n', '\r\n'];

        // Nếu đoạn văn bản đã đủ nhỏ, không cần tìm điểm dừng
        if (end - start <= this.sentenceBreakThreshold) {
            return end;
        }

        // Tìm vị trí dừng từ cuối đoạn ngược về đầu
        for (let i = end; i > start + this.sentenceBreakThreshold; i--) {
            if (endMarks.includes(text[i])) {
                return i;
            }
        }

        // Nếu không tìm thấy dấu kết thúc câu, tìm dấu phẩy
        for (let i = end; i > start + this.sentenceBreakThreshold; i--) {
            if (text[i] === ',') {
                return i;
            }
        }

        // Nếu không tìm thấy, tìm khoảng trắng
        for (let i = end; i > start + this.sentenceBreakThreshold; i--) {
            if (text[i] === ' ') {
                return i;
            }
        }

        // Nếu không tìm thấy gì, trả về điểm giữa
        return start + Math.floor((end - start) / 2);
    }

    /**
     * Phát phần tử tiếp theo trong hàng đợi
     */
    async playNext() {
        if (this.queue.length === 0 || this.isPlaying || this.isPaused) return;

        this.isPlaying = true;
        this.currentText = this.queue.shift();

        try {
            console.log(`Đang đọc đoạn: ${this.currentText.substring(0, 50)}... (${this.completionPercentage}%)`);

            const audioUrl = await this.getAudioFromServer(this.currentText);
            if (!audioUrl) {
                throw new Error('Không nhận được URL audio từ server');
            }

            this.audio.src = audioUrl;
            this.audio.play().catch(error => {
                console.error('Lỗi khi phát audio:', error);
                this.hasError = true;
                this.errorMessage = 'Không thể phát audio';
                if (this.onError) {
                    this.onError(this.errorMessage);
                }
                this.isPlaying = false;
                this.playNext(); // Thử phát phần tử tiếp theo
            });
        } catch (error) {
            console.error('Lỗi khi phát audio:', error);
            this.hasError = true;
            this.errorMessage = error.message || 'Lỗi không xác định';
            if (this.onError) {
                this.onError(this.errorMessage);
            }
            this.isPlaying = false;
            this.playNext(); // Thử phát phần tử tiếp theo
        }
    }

    /**
     * Gửi yêu cầu đến server để chuyển đổi văn bản thành giọng nói
     * @param {string} text - Văn bản để chuyển đổi
     * @returns {Promise<string>} - URL của file audio
     */
    async getAudioFromServer(text) {
        try {
            // Tạo controller để có thể hủy request nếu cần
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), this.apiTimeout);

            const response = await fetch('/api/tts/convert', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    text: text,
                    voice: this.defaultVoice,
                    rate: this.defaultRate,
                    volume: this.defaultVolume
                }),
                signal: controller.signal
            });

            // Xóa timeout vì request đã hoàn thành
            clearTimeout(timeoutId);

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.error || `HTTP error! Status: ${response.status}`);
            }

            const data = await response.json();

            // Kiểm tra nếu văn bản bị cắt ngắn
            if (data.isTruncated) {
                console.warn(`Văn bản bị cắt ngắn: ${data.processedLength}/${data.originalLength} ký tự`);
                if (this.onError) {
                    this.onError(`Văn bản quá dài, chỉ đọc được một phần. Đã xử lý ${data.processedLength}/${data.originalLength} ký tự.`);
                }
            }

            return data.audioUrl;
        } catch (error) {
            // Xử lý lỗi timeout
            if (error.name === 'AbortError') {
                console.error('Request bị hủy do timeout');
                if (this.onError) {
                    this.onError('Quá trình chuyển đổi quá lâu, có thể do văn bản quá dài.');
                }
                return null;
            }

            console.error('Lỗi khi gọi API:', error);
            if (this.onError) {
                this.onError(`Lỗi từ server: ${error.message}`);
            }
            throw error;
        }
    }

    /**
     * Tạm dừng phát audio hiện tại
     */
    pause() {
        if (this.isPlaying && !this.isPaused) {
            this.audio.pause();
            this.isPaused = true;
            this.isPlaying = false;
        }
    }

    /**
     * Tiếp tục phát audio hiện tại
     */
    resume() {
        if (this.isPaused) {
            this.audio.play().catch(error => {
                console.error('Lỗi khi tiếp tục phát audio:', error);
                if (this.onError) {
                    this.onError('Không thể tiếp tục phát audio');
                }
            });
            this.isPaused = false;
            this.isPlaying = true;
        }
    }

    /**
     * Dừng tất cả và xóa hàng đợi
     */
    stop() {
        this.audio.pause();
        this.audio.currentTime = 0;
        this.isPlaying = false;
        this.isPaused = false;
        this.queue = [];
        this.currentText = '';
        this.fullText = '';
        this.currentPosition = 0;
        this.completionPercentage = 0;
        this.hasError = false;
        this.errorMessage = '';

        if (this.onProgressChange) {
            this.onProgressChange(0);
        }
    }

    /**
     * Thiết lập kích thước đoạn văn bản
     * @param {number} size - Kích thước đoạn (số ký tự)
     */
    setChunkSize(size) {
        if (size > 0) {
            this.chunkSize = size;
        }
    }

    /**
     * Thiết lập giọng đọc
     * @param {string} voice - Mã giọng đọc
     */
    setVoice(voice) {
        if (voice) {
            this.defaultVoice = voice;
        }
    }

    /**
     * Thiết lập tốc độ đọc
     * @param {string} rate - Tốc độ đọc, ví dụ: "+10%"
     */
    setRate(rate) {
        if (rate) {
            this.defaultRate = rate;
        }
    }

    /**
     * Thiết lập âm lượng
     * @param {string} volume - Âm lượng, ví dụ: "+10%"
     */
    setVolume(volume) {
        if (volume) {
            this.defaultVolume = volume;
        }
    }

    /**
     * Đăng ký callback để theo dõi tiến độ
     * @param {function} callback - Hàm callback nhận vào giá trị phần trăm tiến độ (0-100)
     */
    registerProgressCallback(callback) {
        if (typeof callback === 'function') {
            this.onProgressChange = callback;
        }
    }

    /**
     * Đăng ký callback khi có lỗi
     * @param {function} callback - Hàm callback nhận vào thông báo lỗi
     */
    registerErrorCallback(callback) {
        if (typeof callback === 'function') {
            this.onError = callback;
        }
    }

    /**
     * Đăng ký callback khi hoàn thành
     * @param {function} callback - Hàm callback được gọi khi đọc xong
     */
    registerCompleteCallback(callback) {
        if (typeof callback === 'function') {
            this.onComplete = callback;
        }
    }

    /**
     * Đăng ký callback khi bắt đầu
     * @param {function} callback - Hàm callback được gọi khi bắt đầu đọc
     */
    registerStartCallback(callback) {
        if (typeof callback === 'function') {
            this.onStart = callback;
        }
    }

    /**
     * Trích xuất văn bản từ phần tử HTML
     * @param {HTMLElement} element - Phần tử HTML để trích xuất văn bản
     * @returns {string} - Văn bản đã trích xuất
     */
    static extractTextFromElement(element) {
        if (!element) return '';

        // Xử lý text node
        if (element.nodeType === 3) { // Node.TEXT_NODE
            return element.textContent.trim();
        }

        // Bỏ qua các phần tử script, style
        if (element.tagName === 'SCRIPT' || element.tagName === 'STYLE') {
            return '';
        }

        // Lấy tất cả văn bản từ các phần tử con
        let textContent = '';
        for (let i = 0; i < element.childNodes.length; i++) {
            textContent += ' ' + EdgeTTS.extractTextFromElement(element.childNodes[i]);
        }

        // Xử lý những phần tử block có thể cần thêm dấu xuống dòng
        if (
            element.tagName === 'DIV' ||
            element.tagName === 'P' ||
            element.tagName === 'H1' ||
            element.tagName === 'H2' ||
            element.tagName === 'H3' ||
            element.tagName === 'H4' ||
            element.tagName === 'H5' ||
            element.tagName === 'H6'
        ) {
            textContent += '\n';
        }

        return textContent.replace(/\s+/g, ' ').trim();
    }
}

// Tạo một instance của EdgeTTS để sử dụng trong trang
const edgeTTS = new EdgeTTS();

// Xuất tham chiếu đến instance toàn cục
window.edgeTTS = edgeTTS; 