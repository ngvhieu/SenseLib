document.addEventListener('DOMContentLoaded', function () {
    // Xử lý menu di động
    const mobileMenuBtn = document.querySelector('.mobile-menu-btn');
    const nav = document.querySelector('nav');

    if (mobileMenuBtn) {
        mobileMenuBtn.addEventListener('click', function () {
            nav.classList.toggle('mobile-active');
        });
    }

    // Xử lý các nút danh mục
    const categoryButtons = document.querySelectorAll('.category-btn');

    if (categoryButtons.length > 0) {
        categoryButtons.forEach(button => {
            button.addEventListener('click', function () {
                // Xóa trạng thái active của tất cả các nút
                categoryButtons.forEach(btn => btn.classList.remove('active'));
                // Thêm trạng thái active cho nút được chọn
                this.classList.add('active');

                // Thêm xử lý lọc tài liệu theo danh mục ở đây
                const category = this.getAttribute('data-category');
                filterDocuments(category);
            });
        });
    }

    // Hàm lọc tài liệu theo danh mục
    function filterDocuments(category) {
        const documents = document.querySelectorAll('.document-card');

        if (documents.length > 0) {
            if (category === 'all') {
                // Hiển thị tất cả tài liệu
                documents.forEach(doc => {
                    doc.style.display = 'block';
                });
            } else {
                // Hiển thị tài liệu theo danh mục
                documents.forEach(doc => {
                    if (doc.getAttribute('data-category') === category) {
                        doc.style.display = 'block';
                    } else {
                        doc.style.display = 'none';
                    }
                });
            }
        }
    }

    // Xử lý nút yêu thích
    const favoriteButtons = document.querySelectorAll('.favorite-btn');

    if (favoriteButtons.length > 0) {
        favoriteButtons.forEach(button => {
            button.addEventListener('click', function (e) {
                e.preventDefault();

                // Chuyển đổi trạng thái yêu thích
                this.classList.toggle('active');
                const documentId = this.getAttribute('data-id');

                // Lưu vào danh sách yêu thích trong localStorage
                toggleFavorite(documentId);

                // Hiển thị thông báo
                showNotification('Đã thêm vào danh sách yêu thích');
            });
        });
    }

    // Hàm chuyển đổi trạng thái yêu thích
    function toggleFavorite(documentId) {
        // Lấy danh sách yêu thích từ localStorage
        let favorites = JSON.parse(localStorage.getItem('favorites')) || [];

        // Kiểm tra xem tài liệu đã có trong danh sách chưa
        const index = favorites.indexOf(documentId);

        if (index === -1) {
            // Nếu chưa có, thêm vào
            favorites.push(documentId);
        } else {
            // Nếu đã có, xóa đi
            favorites.splice(index, 1);
        }

        // Lưu lại vào localStorage
        localStorage.setItem('favorites', JSON.stringify(favorites));
    }

    // Hàm hiển thị thông báo
    function showNotification(message, type = 'success') {
        // Sử dụng element notification có sẵn thay vì tạo mới
        const notification = document.getElementById('notification');
        if (!notification) return; // Thoát nếu không tìm thấy element

        // Xóa các class hiện tại
        notification.classList.remove('success', 'error', 'warning', 'info');

        // Thêm class tương ứng với loại thông báo
        notification.classList.add(type);

        // Cập nhật nội dung
        notification.textContent = message;

        // Hiển thị thông báo
        notification.classList.add('show');

        // Ẩn thông báo sau 3 giây
        setTimeout(() => {
            notification.classList.remove('show');
        }, 3000);
    }

    // Xử lý tìm kiếm
    const searchForm = document.querySelector('.search-bar form');

    if (searchForm) {
        searchForm.addEventListener('submit', function (e) {
            e.preventDefault();

            const searchInput = this.querySelector('input');
            const searchTerm = searchInput.value.trim().toLowerCase();

            if (searchTerm) {
                // Thực hiện tìm kiếm
                searchDocuments(searchTerm);
            }
        });
    }

    // Hàm tìm kiếm tài liệu
    function searchDocuments(term) {
        // Trong một ứng dụng thực, bạn có thể điều hướng đến trang kết quả tìm kiếm
        // hoặc gửi yêu cầu AJAX để tìm kiếm
        window.location.href = `documents.html?search=${encodeURIComponent(term)}`;
    }

    // Xử lý form bình luận
    const commentForm = document.querySelector('.comment-form');

    if (commentForm) {
        commentForm.addEventListener('submit', function (e) {
            e.preventDefault();

            const textarea = this.querySelector('textarea');
            const commentText = textarea.value.trim();

            if (commentText) {
                // Trong ứng dụng thực, bạn sẽ gửi bình luận đến máy chủ
                // Ở đây, chúng ta giả lập việc thêm bình luận vào DOM
                addComment(commentText);

                // Xóa nội dung textarea
                textarea.value = '';
            }
        });
    }

    // Hàm thêm bình luận
    function addComment(text) {
        const commentsList = document.querySelector('.comments-list');

        if (commentsList) {
            // Tạo phần tử bình luận mới
            const newComment = document.createElement('div');
            newComment.className = 'comment';

            // Tạo HTML cho bình luận
            const now = new Date();
            newComment.innerHTML = `
                <div class="comment-header">
                    <span class="comment-author">Người dùng hiện tại</span>
                    <span class="comment-date">${now.toLocaleDateString()}</span>
                </div>
                <div class="comment-body">
                    <p>${text}</p>
                </div>
            `;

            // Thêm vào đầu danh sách bình luận
            commentsList.insertBefore(newComment, commentsList.firstChild);

            // Hiển thị thông báo
            showNotification('Bình luận của bạn đã được đăng');
        }
    }

    // Xử lý tải tài liệu
    const downloadButtons = document.querySelectorAll('.download-btn');

    if (downloadButtons.length > 0) {
        downloadButtons.forEach(button => {
            button.addEventListener('click', function (e) {
                // Trong ứng dụng thực, bạn sẽ điều hướng đến URL tải xuống
                // hoặc mở hộp thoại tải xuống

                // Hiển thị thông báo
                showNotification('Đang bắt đầu tải xuống...');

                // Thêm vào lịch sử giao dịch
                const documentId = this.getAttribute('data-id');
                const documentName = this.getAttribute('data-name');
                addToHistory('download', documentId, documentName);
            });
        });
    }

    // Hàm thêm vào lịch sử giao dịch
    function addToHistory(action, documentId, documentName) {
        // Lấy lịch sử từ localStorage
        let history = JSON.parse(localStorage.getItem('transaction_history')) || [];

        // Thêm giao dịch mới
        history.push({
            action: action,
            documentId: documentId,
            documentName: documentName,
            timestamp: new Date().toISOString()
        });

        // Lưu lại vào localStorage
        localStorage.setItem('transaction_history', JSON.stringify(history));
    }

    // Hiển thị lịch sử giao dịch nếu đang ở trang lịch sử
    const historyContainer = document.querySelector('.history-container');

    if (historyContainer) {
        displayTransactionHistory();
    }

    // Hàm hiển thị lịch sử giao dịch
    function displayTransactionHistory() {
        const historyList = document.querySelector('.history-list');

        if (historyList) {
            // Lấy lịch sử từ localStorage
            const history = JSON.parse(localStorage.getItem('transaction_history')) || [];

            if (history.length > 0) {
                // Sắp xếp theo thời gian giảm dần (mới nhất lên đầu)
                history.sort((a, b) => new Date(b.timestamp) - new Date(a.timestamp));

                // Tạo HTML cho mỗi mục lịch sử
                historyList.innerHTML = history.map(item => {
                    const date = new Date(item.timestamp).toLocaleDateString();
                    const time = new Date(item.timestamp).toLocaleTimeString();

                    let actionText = '';
                    switch (item.action) {
                        case 'download':
                            actionText = 'Tải xuống';
                            break;
                        case 'view':
                            actionText = 'Xem';
                            break;
                        case 'favorite':
                            actionText = 'Yêu thích';
                            break;
                    }

                    return `
                        <div class="history-item">
                            <div class="history-details">
                                <h4>${item.documentName}</h4>
                                <p>${actionText}</p>
                            </div>
                            <div class="history-date">
                                <p>${date}</p>
                                <p>${time}</p>
                            </div>
                        </div>
                    `;
                }).join('');
            } else {
                historyList.innerHTML = '<p>Không có lịch sử giao dịch.</p>';
            }
        }
    }

    // Hiển thị tài liệu yêu thích nếu đang ở trang yêu thích
    const favoritesContainer = document.querySelector('.favorites-container');

    if (favoritesContainer) {
        displayFavorites();
    }

    // Hàm hiển thị tài liệu yêu thích
    function displayFavorites() {
        // Trong ứng dụng thực, bạn sẽ tải dữ liệu từ máy chủ
        // Ở đây, chúng ta giả lập bằng cách sử dụng localStorage

        const favoritesGrid = document.querySelector('.favorites-grid');

        if (favoritesGrid) {
            // Lấy danh sách ID tài liệu yêu thích từ localStorage
            const favoriteIds = JSON.parse(localStorage.getItem('favorites')) || [];

            if (favoriteIds.length > 0) {
                // Giả lập việc hiển thị tài liệu yêu thích
                // Trong ứng dụng thực, bạn sẽ tải dữ liệu từ máy chủ dựa trên ID
                favoritesGrid.innerHTML = '<p>Đang tải tài liệu yêu thích...</p>';

                // Giả lập việc tải dữ liệu
                setTimeout(() => {
                    favoritesGrid.innerHTML = favoriteIds.map(id => `
                        <div class="document-card" data-id="${id}">
                            <img src="img/document-${id}.jpg" alt="Tài liệu ${id}">
                            <div class="document-card-content">
                                <h3>Tài liệu mẫu ${id}</h3>
                                <div class="document-meta">
                                    <span>Tác giả: Nguyễn Văn A</span>
                                    <span>22/05/2023</span>
                                </div>
                                <p>Đây là mô tả ngắn về tài liệu mẫu ${id}...</p>
                                <a href="document-details.html?id=${id}" class="btn">Xem chi tiết</a>
                            </div>
                        </div>
                    `).join('');
                }, 500);
            } else {
                favoritesGrid.innerHTML = '<p>Bạn chưa có tài liệu yêu thích nào.</p>';
            }
        }
    }
}); 