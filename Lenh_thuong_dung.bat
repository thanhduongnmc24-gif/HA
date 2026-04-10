

git add . && git commit -m "Cập nhật" && git push origin main

tạo thư mục out để lưu file exe
    dotnet publish HA/HA.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o out 

cách làm bỏ qua tệp >100Mb
    # 1. Mở gói bưu kiện vừa đóng ra (Hủy commit vừa rồi, nhưng KHÔNG làm mất code anh hai đã viết)
        git reset HEAD~1

    # 2. Xóa sổ hoàn toàn cái thư mục rác "D:" đang nằm lỳ trong máy ảo
        rm -rf "D:"
        rm -rf "D:/"

    # 3. Gỡ bỏ nó khỏi trí nhớ của Git
        git rm -r --cached "D:" 2>/dev/null

    # 4. Đóng gói lại từ đầu (lúc này thằng D: đã chết nên không thể chui vào bưu kiện được nữa)
        it add .
        git commit -m "Dọn dẹp triệt để thư mục ảo D: và file rác 155MB"

    # 5. Gửi lên mây
        git push origin main