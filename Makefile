all: out/wisp

out/:
	mkdir -p $@

out/wisp: wisp.fs | out/
	./wisp.fs

dump: out/wisp
	objdump -b binary \
    --adjust-vma=0x400000 \
    -m i386:x86-64 \
    --start-address=0x400078 \
    -D out/wisp
	ls -lh out/wisp

clean:
	rm -rf out/
